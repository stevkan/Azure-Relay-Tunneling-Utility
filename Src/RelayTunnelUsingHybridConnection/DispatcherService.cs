using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Collections.Generic;
using RelayTunnelUsingHybridConnection.Extensions;
using Microsoft.Azure.Relay;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RelayTunnelUsingHybridConnection

{
    internal class DispatcherService
    {
        private readonly HttpClient _httpClient;
        private readonly string _hybridConnectionSubPath;
        private readonly HybridConnectionListener _listener;
        private readonly Uri _targetServiceAddress;
        private readonly Uri _targetWebSocketAddress;
        private readonly RelayConfig _config;
        private readonly RelayResourceManager _resourceManager;
        private readonly WebSocketForwarder _webSocketForwarder;

        public DispatcherService(RelayConfig config, RelayResourceManager resourceManager = null)
        {
            _config = config;
            _resourceManager = resourceManager;
            _targetServiceAddress = new Uri(config.TargetServiceAddress);

            // Set up WebSocket target address - use TargetWebSocketAddress if specified, otherwise derive from TargetServiceAddress
            if (!string.IsNullOrEmpty(config.TargetWebSocketAddress))
            {
                _targetWebSocketAddress = new Uri(config.TargetWebSocketAddress);
            }
            else
            {
                // Convert HTTP address to WebSocket address
                var wsScheme = _targetServiceAddress.Scheme == "https" ? "wss" : "ws";
                _targetWebSocketAddress = new Uri($"{wsScheme}://{_targetServiceAddress.Authority}{_targetServiceAddress.PathAndQuery}");
            }

            var tokenProvider = TokenProvider.CreateSharedAccessSignatureTokenProvider(config.PolicyName, config.PolicyKey);
            _listener = new HybridConnectionListener(new Uri($"sb://{config.RelayNamespace}/{config.RelayName}"), tokenProvider);

            var handler = new HttpClientHandler 
            {
                UseCookies = false,
                AllowAutoRedirect = false
            };
            
            // For localhost HTTPS, bypass SSL certificate validation
            if (_targetServiceAddress.IsLoopback && _targetServiceAddress.Scheme == "https")
            {
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
            }
            
            _httpClient = new HttpClient(handler)
            {
                BaseAddress = _targetServiceAddress,
                Timeout = TimeSpan.FromSeconds(30)
            };
            _httpClient.DefaultRequestHeaders.ExpectContinue = false;
            _httpClient.DefaultRequestHeaders.ConnectionClose = false;

            _hybridConnectionSubPath = _listener.Address.AbsolutePath.EnsureEndsWith("/");
            
            // Initialize WebSocket forwarder if WebSocket support is enabled
            if (config.EnableWebSocketSupport)
            {
                _webSocketForwarder = new WebSocketForwarder(_targetWebSocketAddress);
            }
        }

        public async Task OpenAsync(CancellationToken cancelToken)
        {
            // Create the hybrid connection resource dynamically if requested
            if (_config.DynamicResourceCreation && _resourceManager != null)
            {
                var created = await _resourceManager.CreateHybridConnectionAsync(_config);
                if (!created)
                {
                    throw new InvalidOperationException($"Failed to create hybrid connection '{_config.RelayName}'");
                }
                
                // Small delay to allow resource to be fully available
                await Task.Delay(2000, cancelToken);
            }

            _listener.RequestHandler = ListenerRequestHandler;
            await _listener.OpenAsync(cancelToken);
            Console.WriteLine("Azure Relay is listening on \n\r\t{0}\n\rand routing requests to \n\r\t{1}", _listener.Address, _httpClient.BaseAddress);
            
            if (_config.EnableWebSocketSupport && _webSocketForwarder != null)
            {
                Console.WriteLine("WebSocket forwarding enabled, routing to \n\r\t{0}", _targetWebSocketAddress);
                // Start WebSocket accept loop
                _ = Task.Run(() => AcceptWebSocketsAsync(cancelToken));
            }
            
            Console.WriteLine("\n\rPress [Enter] to exit");
        }

        public async Task CloseAsync(CancellationToken cancelToken)
        {
            await _listener.CloseAsync(cancelToken);
            _httpClient.Dispose();
            
            // Delete the hybrid connection resource if it was dynamically created
            if (_config.DynamicResourceCreation && _resourceManager != null)
            {
                Console.WriteLine($"Dynamic resource deletion enabled for '{_config.RelayName}'");
                var deleted = await _resourceManager.DeleteHybridConnectionAsync(_config);
                if (!deleted)
                {
                    Console.WriteLine($"⚠ Warning: Failed to delete hybrid connection '{_config.RelayName}'");
                }
            }
        }

        private async void ListenerRequestHandler(RelayedHttpListenerContext context)
        {
            var startTimeUtc = DateTime.UtcNow;
            try
            {
                        // Handle as regular HTTP request
                // Note: WebSocket connections are handled separately via AcceptConnectionAsync loop
                await HandleHttpRequestAsync(context);
            }
            catch (Exception ex)
            {
                LogException(ex);
                try 
                {
                    context.Response.StatusCode = HttpStatusCode.InternalServerError;
                    context.Response.StatusDescription = "Internal Server Error";
                    await context.Response.CloseAsync();
                }
                catch (ObjectDisposedException)
                {
                    // Response already disposed/closed - ignore
                }
                catch (InvalidOperationException)
                {
                    // Response already closed - ignore
                }
            }
            finally
            {
                LogRequest(startTimeUtc);
            }
        }

        private async Task AcceptWebSocketsAsync(CancellationToken ct)
        {
            Console.WriteLine("WebSocket accept loop starting...");
            while (!ct.IsCancellationRequested)
            {
                HybridConnectionStream relayStream = null;
                try
                {
                    relayStream = await _listener.AcceptConnectionAsync();
                    if (relayStream == null) break; // listener closed

                    Console.WriteLine("WebSocket connection accepted, forwarding to target service...");
                    
                    // Forward this connection to the backend WebSocket
                    _ = Task.Run(() => _webSocketForwarder.ForwardAsync(relayStream, _targetWebSocketAddress, ct));
                }
                catch (OperationCanceledException) 
                { 
                    break; 
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"WebSocket accept loop error: {ex.Message}");
                    try { relayStream?.Dispose(); } catch { }
                }
            }
            Console.WriteLine("WebSocket accept loop stopped.");
        }

        private async Task HandleHttpRequestAsync(RelayedHttpListenerContext context)
        {
            var originalPath = context.Request.Url.GetComponents(UriComponents.PathAndQuery, UriFormat.Unescaped);
            var relativePath = originalPath.Replace(_hybridConnectionSubPath, string.Empty, StringComparison.OrdinalIgnoreCase);
            
            try
            {
                // Handle SPA and API routes
                if (await HandleSpaRoutesAsync(context, relativePath))
                {
                    // SPA routes handle their own response closing
                    return; 
                }

                // Fall back to proxying to target service
                Console.WriteLine("Proxying to {0}...", _targetServiceAddress);
                var requestMessage = CreateHttpRequestMessage(context);
                
                var responseMessage = await _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);
                await SendResponseAsync(context, responseMessage);
                responseMessage.Dispose();
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("ResponseEnded"))
            {
                Console.WriteLine($"Target service connection closed prematurely. Is {_targetServiceAddress} running?");
                context.Response.StatusCode = HttpStatusCode.BadGateway;
                context.Response.StatusDescription = "Target service unavailable";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling request: {ex.Message}");
                context.Response.StatusCode = HttpStatusCode.InternalServerError;
                context.Response.StatusDescription = "Internal server error";
            }
            
            // Only close if we didn't handle it via SPA routes
            await context.Response.CloseAsync();
        }

        private async Task<bool> HandleSpaRoutesAsync(RelayedHttpListenerContext context, string relativePath)
        {
            // Serve SPA for root path or any non-API path
            if (string.IsNullOrEmpty(relativePath) || relativePath == "/")
            {
                await ServeSpaAsync(context);
                return true;
            }
            
            // Handle API routes (with or without leading slash)
            if (relativePath.StartsWith("api/list", StringComparison.OrdinalIgnoreCase) || 
                relativePath.StartsWith("/api/list", StringComparison.OrdinalIgnoreCase))
            {
                await HandleDirectoryListApiAsync(context, relativePath);
                return true;
            }
            
            if (relativePath.StartsWith("file", StringComparison.OrdinalIgnoreCase) || 
                relativePath.StartsWith("/file", StringComparison.OrdinalIgnoreCase))
            {
                await HandleFileDownloadAsync(context, relativePath);
                return true;
            }

            // For any other path, serve the SPA (client-side routing)
            if (!relativePath.Contains(".") && context.Request.HttpMethod == "GET")
            {
                await ServeSpaAsync(context);
                return true;
            }

            return false; // Let it fall through to proxy
        }

        private async Task ServeSpaAsync(RelayedHttpListenerContext context)
        {
            var htmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "directory-browser.html");
            
            if (File.Exists(htmlPath))
            {
                var html = await File.ReadAllTextAsync(htmlPath);
                var bytes = Encoding.UTF8.GetBytes(html);
                
                context.Response.StatusCode = HttpStatusCode.OK;
                context.Response.Headers["Content-Type"] = "text/html; charset=utf-8";
                context.Response.Headers["Cache-Control"] = "no-cache";
                context.Response.Headers["Content-Length"] = bytes.Length.ToString();
                
                await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
                await context.Response.OutputStream.FlushAsync();
            }
            else
            {

                context.Response.StatusCode = HttpStatusCode.NotFound;
                var error = Encoding.UTF8.GetBytes("SPA file not found");
                context.Response.Headers["Content-Type"] = "text/plain";
                context.Response.Headers["Content-Length"] = error.Length.ToString();
                await context.Response.OutputStream.WriteAsync(error, 0, error.Length);
                await context.Response.OutputStream.FlushAsync();
            }
            
            // Close the response properly
            await context.Response.CloseAsync();
        }

        private async Task HandleDirectoryListApiAsync(RelayedHttpListenerContext context, string relativePath)
        {
            try
            {
                // Extract path parameter from query string
                var query = context.Request.Url.Query;
                var pathParam = "";
                
                if (!string.IsNullOrEmpty(query) && query.StartsWith("?"))
                {
                    // Simple query string parsing for path parameter
                    var pairs = query.Substring(1).Split('&');
                    foreach (var pair in pairs)
                    {
                        var keyValue = pair.Split('=');
                        if (keyValue.Length == 2 && keyValue[0] == "path")
                        {
                            pathParam = Uri.UnescapeDataString(keyValue[1]);
                            break;
                        }
                    }
                }

                // Make request to file server to get directory listing
                var baseUrl = _targetServiceAddress.ToString().TrimEnd('/');
                var targetUrl = $"{baseUrl}/{pathParam}";
                var response = await _httpClient.GetAsync(targetUrl);
                
                if (response.IsSuccessStatusCode)
                {
                    var html = await response.Content.ReadAsStringAsync();
                    var items = ParseDirectoryListing(html);
                    var json = JsonConvert.SerializeObject(new { items });
                    
                    context.Response.StatusCode = HttpStatusCode.OK;
                    context.Response.Headers["Content-Type"] = "application/json";
                    context.Response.Headers["Cache-Control"] = "no-cache";
                    
                    var bytes = Encoding.UTF8.GetBytes(json);
                    await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
                }
                else
                {
                    context.Response.StatusCode = response.StatusCode;
                    var error = JsonConvert.SerializeObject(new { error = "Directory not found" });
                    var bytes = Encoding.UTF8.GetBytes(error);
                    await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in directory API: {ex.Message}");
                context.Response.StatusCode = HttpStatusCode.InternalServerError;
                var error = JsonConvert.SerializeObject(new { error = "Internal server error" });
                var bytes = Encoding.UTF8.GetBytes(error);
                await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
            }
            
            await context.Response.CloseAsync();
        }

        private async Task HandleFileDownloadAsync(RelayedHttpListenerContext context, string relativePath)
        {
            try
            {
                // Extract path parameter from query string
                var query = context.Request.Url.Query;
                var pathParam = "";
                
                if (!string.IsNullOrEmpty(query) && query.StartsWith("?"))
                {
                    // Simple query string parsing for path parameter
                    var pairs = query.Substring(1).Split('&');
                    foreach (var pair in pairs)
                    {
                        var keyValue = pair.Split('=');
                        if (keyValue.Length == 2 && keyValue[0] == "path")
                        {
                            pathParam = Uri.UnescapeDataString(keyValue[1]);
                            break;
                        }
                    }
                }

                // Proxy file request to target service
                var baseUrl = _targetServiceAddress.ToString().TrimEnd('/');
                var targetUrl = $"{baseUrl}/{pathParam}";
                var response = await _httpClient.GetAsync(targetUrl);
                
                context.Response.StatusCode = response.StatusCode;
                foreach (var header in response.Headers)
                {
                    if (!string.Equals(header.Key, "Transfer-Encoding", StringComparison.OrdinalIgnoreCase))
                    {
                        context.Response.Headers.Add(header.Key, string.Join(",", header.Value));
                    }
                }

                if (response.Content != null)
                {
                    foreach (var header in response.Content.Headers)
                    {
                        if (!string.Equals(header.Key, "Content-Length", StringComparison.OrdinalIgnoreCase))
                        {
                            context.Response.Headers.Add(header.Key, string.Join(",", header.Value));
                        }
                    }
                    
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    await responseStream.CopyToAsync(context.Response.OutputStream);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in file download: {ex.Message}");
                context.Response.StatusCode = HttpStatusCode.InternalServerError;
            }
            
            await context.Response.CloseAsync();
        }

        private List<object> ParseDirectoryListing(string html)
        {
            var items = new List<object>();
            
            try
            {
                // Simple regex-based parsing of the HTML directory listing
                // Look for anchor tags with href attributes
                var linkPattern = @"<a\s+href=""([^""]+)""\s+class=""[^""]*icon[^""]*icon-([^""]*)""\s+title=""([^""]+)""[^>]*>.*?<span\s+class=""name"">([^<]+)</span>";
                var matches = System.Text.RegularExpressions.Regex.Matches(html, linkPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                
                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    var href = match.Groups[1].Value;
                    var iconClass = match.Groups[2].Value;
                    var title = match.Groups[3].Value;
                    var name = match.Groups[4].Value;
                    
                    // Skip parent directory entries and system files
                    if (name == ".." || name.StartsWith(".")) continue;
                    
                    var type = iconClass.Contains("directory") ? "directory" : "file";
                    
                    items.Add(new
                    {
                        name = name,
                        type = type,
                        href = href,
                        title = title
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing directory listing: {ex.Message}");
            }
            
            return items;
        }

        private async Task SendResponseAsync(RelayedHttpListenerContext context, HttpResponseMessage responseMessage)
        {
            context.Response.StatusCode = responseMessage.StatusCode;
            context.Response.StatusDescription = responseMessage.ReasonPhrase;
            
            // Copy response headers
            foreach (var header in responseMessage.Headers)
            {
                if (string.Equals(header.Key, "Transfer-Encoding"))
                {
                    continue;
                }

                context.Response.Headers.Add(header.Key, string.Join(",", header.Value));
            }

            // Copy content headers (including Content-Type)
            if (responseMessage.Content != null)
            {
                foreach (var header in responseMessage.Content.Headers)
                {
                    if (string.Equals(header.Key, "Content-Length"))
                    {
                        continue; // This will be set automatically
                    }
                    
                    context.Response.Headers.Add(header.Key, string.Join(",", header.Value));
                }
            }

            var responseStream = await responseMessage.Content.ReadAsStreamAsync();
            await responseStream.CopyToAsync(context.Response.OutputStream);
        }

        private void SendErrorResponse(Exception ex, RelayedHttpListenerContext context)
        {
            context.Response.StatusCode = HttpStatusCode.InternalServerError;
            context.Response.StatusDescription = $"Internal Server Error: {ex.GetType().FullName}: {ex.Message}";
            context.Response.Close();
        }

        private HttpRequestMessage CreateHttpRequestMessage(RelayedHttpListenerContext context)
        {
            var requestMessage = new HttpRequestMessage();
            if (context.Request.HasEntityBody)
            {
                requestMessage.Content = new StreamContent(context.Request.InputStream);
                // Experiment to see if I can capture the return message instead of having the bot responding directly (so far it doesn't work).
                //var contentStream = new MemoryStream();
                //var writer = new StreamWriter(contentStream);
                //var newActivity = requestMessage.Content.ReadAsStringAsync().Result.Replace("https://directline.botframework.com/", "https://localhost:44372/");
                //writer.Write(newActivity);
                //writer.Flush();
                //contentStream.Position = 0;
                //requestMessage.Content = new StreamContent(contentStream);
                var contentType = context.Request.Headers[HttpRequestHeader.ContentType];
                if (!string.IsNullOrEmpty(contentType))
                {
                    requestMessage.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
                }
            }

            var relativePath = context.Request.Url.GetComponents(UriComponents.PathAndQuery, UriFormat.Unescaped);
            relativePath = relativePath.Replace(_hybridConnectionSubPath, string.Empty, StringComparison.OrdinalIgnoreCase);
            
            requestMessage.RequestUri = new Uri(relativePath, UriKind.RelativeOrAbsolute);
            requestMessage.Method = new HttpMethod(context.Request.HttpMethod);

            foreach (var headerName in context.Request.Headers.AllKeys)
            {
                if (string.Equals(headerName, "Host", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(headerName, "Content-Type", StringComparison.OrdinalIgnoreCase))
                {
                    // Don't flow these headers here
                    continue;
                }

                requestMessage.Headers.Add(headerName, context.Request.Headers[headerName]);
            }

            LogRequestActivity(requestMessage);

            return requestMessage;
        }

        private void LogRequest(DateTime startTimeUtc)
        {
            var stopTimeUtc = DateTime.UtcNow;
            //var buffer = new StringBuilder();
            //buffer.Append($"{startTimeUtc.ToString("s", CultureInfo.InvariantCulture)}, ");
            //buffer.Append($"\"{context.Request.HttpMethod} {context.Request.Url.GetComponents(UriComponents.PathAndQuery, UriFormat.Unescaped)}\", ");
            //buffer.Append($"{(int)context.Response.StatusCode}, ");
            //buffer.Append($"{(int)stopTimeUtc.Subtract(startTimeUtc).TotalMilliseconds}");
            //Console.WriteLine(buffer);

            Console.WriteLine("...and back {0:N0} ms...", stopTimeUtc.Subtract(startTimeUtc).TotalMilliseconds);
            Console.WriteLine("");
        }

        private void LogRequestActivity(HttpRequestMessage requestMessage)
        {
            var content = requestMessage.Content?.ReadAsStringAsync().Result ?? "";
            Console.ForegroundColor = ConsoleColor.Yellow;

            var formatted = content;
            if (!string.IsNullOrEmpty(formatted) && IsValidJson(formatted))
            {
                var s = new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented
                };

                dynamic o = JsonConvert.DeserializeObject(content);
                formatted = JsonConvert.SerializeObject(o, s);
            }

            if (!string.IsNullOrEmpty(formatted))
            {
                Console.WriteLine(formatted);
            }
            else
            {
                Console.WriteLine($"{requestMessage.Method} {requestMessage.RequestUri} (no content)");
            }
            Console.ResetColor();
        }

        private static void LogException(Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(ex);
            Console.WriteLine("");
            Console.ResetColor();
        }

        private static bool IsValidJson(string strInput)
        {
            strInput = strInput.Trim();
            if ((!strInput.StartsWith("{") || !strInput.EndsWith("}")) && (!strInput.StartsWith("[") || !strInput.EndsWith("]")))
            {
                return false;
            }

            try
            {
                JToken.Parse(strInput);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}