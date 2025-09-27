using System;
using System.IO;
using System.Net.Http;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.Threading.Tasks;

namespace RelayTunnelUsingWCF
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall)]
    public class ConfigurableRelayProxyService : IRelayProxyService, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly Uri _targetServiceAddress;
        private readonly RelayConfiguration _config;

        public ConfigurableRelayProxyService(RelayConfiguration config)
        {
            _config = config;
            
            if (string.IsNullOrEmpty(config.TargetServiceAddress))
            {
                throw new InvalidOperationException("TargetServiceAddress not configured");
            }

            _targetServiceAddress = new Uri(config.TargetServiceAddress);
            _httpClient = new HttpClient();
            
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

                // Build the target URL preserving the original path and query
                var originalUri = incomingRequest.UriTemplateMatch.RequestUri;
                var targetUri = new Uri(_targetServiceAddress, originalUri.PathAndQuery);

                if (_config.EnableDetailedLogging)
                {
                    Console.WriteLine($"[{_config.RelayName}] Proxying {incomingRequest.Method} {originalUri.PathAndQuery} to {targetUri}");
                }

                // Create the proxy request
                var proxyRequest = new HttpRequestMessage(new HttpMethod(incomingRequest.Method), targetUri);

                // Copy headers (except restricted ones)
                foreach (var headerName in incomingRequest.Headers.AllKeys)
                {
                    if (IsRestrictedHeader(headerName))
                        continue;

                    var headerValue = incomingRequest.Headers[headerName];
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
                if (requestBody != null && requestBody.CanRead && incomingRequest.ContentLength > 0)
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
                            foreach (var value in header.Value)
                            {
                                outgoingResponse.Headers.Add(header.Key, value);
                            }
                        }

                        if (response.Content != null)
                        {
                            foreach (var header in response.Content.Headers)
                            {
                                foreach (var value in header.Value)
                                {
                                    outgoingResponse.Headers.Add(header.Key, value);
                                }
                            }

                            // Return response content as stream
                            var responseContent = await response.Content.ReadAsByteArrayAsync();
                            return new MemoryStream(responseContent);
                        }

                        return new MemoryStream();
                    }
                });

                return task.Result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{_config.RelayName}] Error proxying request: {ex.Message}");
                
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

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
