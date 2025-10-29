using System;
using System.IO;
using System.Net.Http;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RelayTunnelUsingWCF
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall)]
    public class ConfigurableRelayProxyService : IRelayProxyService, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly Uri _targetServiceAddress;
        private readonly RelayConfiguration _config;
        private readonly SpaService _spaService;

        public ConfigurableRelayProxyService(RelayConfiguration config)
        {
            _config = config;
            
            if (string.IsNullOrEmpty(config.TargetServiceAddress))
            {
                throw new InvalidOperationException("TargetServiceAddress not configured");
            }

            _targetServiceAddress = new Uri(config.TargetServiceAddress);
            _httpClient = new HttpClient();
            _spaService = new SpaService(config, _httpClient);
            
            if (config.EnableDetailedLogging)
            {
                Console.WriteLine($"Relay proxy initialized for '{config.RelayName}'. Target: {_targetServiceAddress}");
            }
        }

        public Stream ProxyRequest(Stream requestBody)
        {
            try
            {
                var context = WebOperationContext.Current;
                if (context == null)
                {
                    throw new InvalidOperationException("No web operation context available");
                }

                var incomingRequest = context.IncomingRequest;
                var outgoingResponse = context.OutgoingResponse;

                // Build the target URL, stripping the relay name from the path
                var originalUri = incomingRequest.UriTemplateMatch.RequestUri;
                var relayPath = "/" + _config.RelayName + "/";
                var pathAndQuery = originalUri.PathAndQuery;
                
                // Handle SPA routes (these methods set their own content types)
                var routeResult = HandleSpaRoutes(pathAndQuery, relayPath);
                if (routeResult != null)
                {
                    // Ensure content type is not overridden by WCF
                    // The SPA route handlers have already set the appropriate content type
                    return routeResult;
                }
                
                // Remove the relay name prefix if present
                if (pathAndQuery.StartsWith(relayPath, StringComparison.OrdinalIgnoreCase))
                {
                    pathAndQuery = pathAndQuery.Substring(relayPath.Length - 1);
                }
                else if (pathAndQuery.Equals("/" + _config.RelayName, StringComparison.OrdinalIgnoreCase))
                {
                    pathAndQuery = "/";
                }
                
                var targetUri = new Uri(_targetServiceAddress.ToString().TrimEnd('/') + pathAndQuery);

                if (_config.EnableDetailedLogging)
                {
                    Console.WriteLine($"[{_config.RelayName}] Proxying {incomingRequest.Method} {originalUri.PathAndQuery} to {targetUri}");
                }

                // Create the proxy request
                var proxyRequest = new HttpRequestMessage(new HttpMethod(incomingRequest.Method), targetUri);

                // Copy headers (except restricted ones)
                foreach (var headerName in incomingRequest.Headers.AllKeys)
                {
                    if (string.IsNullOrEmpty(headerName) || IsRestrictedHeader(headerName))
                        continue;

                    var headerValue = incomingRequest.Headers[headerName];
                    if (string.IsNullOrEmpty(headerValue))
                        continue;

                    if (!proxyRequest.Headers.TryAddWithoutValidation(headerName, headerValue))
                    {
                        // If it fails to add to request headers, try content headers
                        if (proxyRequest.Content == null && requestBody != null)
                        {
                            proxyRequest.Content = new StreamContent(requestBody);
                        }
                        if (proxyRequest.Content != null)
                        {
                            proxyRequest.Content.Headers.TryAddWithoutValidation(headerName, headerValue);
                        }
                    }
                }

                // Set request content if we have a body
                var hasContentLength = !string.IsNullOrEmpty(incomingRequest.Headers["Content-Length"]);
                var contentLength = hasContentLength ? incomingRequest.ContentLength : 0;
                
                if (requestBody != null && requestBody.CanRead && contentLength > 0)
                {
                    proxyRequest.Content = new StreamContent(requestBody);
                    
                    // Set content type if available
                    var contentType = incomingRequest.ContentType;
                    if (!string.IsNullOrEmpty(contentType))
                    {
                        proxyRequest.Content.Headers.TryAddWithoutValidation("Content-Type", contentType);
                    }
                }

                // Send the request and get response
                var task = Task.Run(async () =>
                {
                    using (var response = await _httpClient.SendAsync(proxyRequest))
                    {
                        // Set response status
                        outgoingResponse.StatusCode = response.StatusCode;

                        // Copy response headers
                        foreach (var header in response.Headers)
                        {
                            if (string.IsNullOrEmpty(header.Key))
                                continue;

                            foreach (var value in header.Value)
                            {
                                if (!string.IsNullOrEmpty(value))
                                    outgoingResponse.Headers.Add(header.Key, value);
                            }
                        }

                        if (response.Content != null)
                        {
                            // Get content type before processing headers
                            var contentType = response.Content.Headers.ContentType?.MediaType ?? "";
                            var isHtml = contentType.Contains("text/html");

                            // Read response content
                            var responseBytes = await response.Content.ReadAsByteArrayAsync();

                            // Rewrite HTML content to fix absolute paths
                            if (isHtml && responseBytes.Length > 0)
                            {
                                try
                                {
                                    var html = Encoding.UTF8.GetString(responseBytes);
                                    var rewrittenHtml = RewriteHtmlPaths(html, _config.RelayName);
                                    responseBytes = Encoding.UTF8.GetBytes(rewrittenHtml);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"[{_config.RelayName}] Warning: HTML rewriting failed: {ex.Message}");
                                    // Continue with original content on error
                                }
                            }

                            // Copy content headers (update Content-Length with potentially modified content)
                            foreach (var header in response.Content.Headers)
                            {
                                if (string.IsNullOrEmpty(header.Key))
                                    continue;

                                // Skip Content-Length, we'll set it based on actual content
                                if (header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                                    continue;

                                foreach (var value in header.Value)
                                {
                                    if (!string.IsNullOrEmpty(value))
                                        outgoingResponse.Headers.Add(header.Key, value);
                                }
                            }

                            // Set correct Content-Length
                            outgoingResponse.Headers["Content-Length"] = responseBytes.Length.ToString();

                            // Return response content as stream
                            return new MemoryStream(responseBytes);
                        }

                        return new MemoryStream();
                    }
                });

                return task.Result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{_config.RelayName}] Error proxying request: {ex.Message}");
                Console.WriteLine($"[{_config.RelayName}] Stack trace: {ex.StackTrace}");
                
                // Return error response
                var context = WebOperationContext.Current;
                if (context != null)
                {
                    context.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.BadGateway;
                    context.OutgoingResponse.ContentType = "text/plain";
                }

                var errorMessage = $"Relay proxy error: {ex.Message}";
                return new MemoryStream(Encoding.UTF8.GetBytes(errorMessage));
            }
        }

        private static bool IsRestrictedHeader(string headerName)
        {
            // These headers are restricted by HttpClient and shouldn't be copied manually
            var restrictedHeaders = new[]
            {
                "Host", "Content-Length", "Date", "Expect", "If-Modified-Since",
                "Range", "Referer", "Transfer-Encoding", "User-Agent", "Proxy-Connection"
            };

            return Array.Exists(restrictedHeaders, h => 
                string.Equals(h, headerName, StringComparison.OrdinalIgnoreCase));
        }

        private Stream HandleSpaRoutes(string pathAndQuery, string relayPath)
        {
            // Strip relay name from path
            var relativePath = pathAndQuery;
            if (relativePath.StartsWith(relayPath, StringComparison.OrdinalIgnoreCase))
            {
                relativePath = relativePath.Substring(relayPath.Length - 1);
            }

            // Serve SPA for root path
            if (string.IsNullOrEmpty(relativePath) || relativePath == "/" || relativePath.StartsWith("/?"))
            {
                return _spaService.ServeSpaHtml();
            }

            // Handle API routes
            if (relativePath.StartsWith("/api/list", StringComparison.OrdinalIgnoreCase))
            {
                var queryIndex = relativePath.IndexOf('?');
                var path = "";
                if (queryIndex > 0)
                {
                    var query = relativePath.Substring(queryIndex + 1);
                    var pairs = query.Split('&');
                    foreach (var pair in pairs)
                    {
                        var parts = pair.Split('=');
                        if (parts.Length == 2 && parts[0] == "path")
                        {
                            path = Uri.UnescapeDataString(parts[1]);
                            break;
                        }
                    }
                }

                var task = Task.Run(async () => await _spaService.HandleDirectoryListAsync(path));
                return task.Result;
            }

            if (relativePath.StartsWith("/file", StringComparison.OrdinalIgnoreCase))
            {
                var queryIndex = relativePath.IndexOf('?');
                var path = "";
                if (queryIndex > 0)
                {
                    var query = relativePath.Substring(queryIndex + 1);
                    var pairs = query.Split('&');
                    foreach (var pair in pairs)
                    {
                        var parts = pair.Split('=');
                        if (parts.Length == 2 && parts[0] == "path")
                        {
                            path = Uri.UnescapeDataString(parts[1]);
                            break;
                        }
                    }
                }

                var task = Task.Run(async () => await _spaService.HandleFileDownloadAsync(path));
                return task.Result;
            }

            // For any path without an extension and GET method, serve SPA (client-side routing)
            var context = WebOperationContext.Current;
            if (!relativePath.Contains(".") && context.IncomingRequest.Method == "GET")
            {
                return _spaService.ServeSpaHtml();
            }

            // Not a SPA route, return null to continue with proxy
            return null;
        }

        private string RewriteHtmlPaths(string html, string relayName)
        {
            var relayPath = "/" + relayName;

            // Rewrite absolute paths in common HTML attributes
            // Matches: href="/path", src="/path", action="/path", etc.
            // Doesn't rewrite if already prefixed with relay name or if it's a full URL
            var pattern = @"((?:href|src|action|data|content)=['""])(/(?!" + Regex.Escape(relayName) + @"/|/|[a-z]+:))";
            var replacement = "$1" + relayPath + "$2";

            var result = Regex.Replace(html, pattern, replacement, RegexOptions.IgnoreCase);

            if (_config.EnableDetailedLogging && result != html)
            {
                Console.WriteLine($"[{_config.RelayName}] Rewrote HTML paths with base: {relayPath}");
            }

            return result;
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
