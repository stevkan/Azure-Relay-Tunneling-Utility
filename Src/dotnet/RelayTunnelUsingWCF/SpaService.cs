using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.ServiceModel.Web;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RelayTunnelUsingWCF
{
    public class SpaService
    {
        private readonly RelayConfiguration _config;
        private readonly HttpClient _httpClient;

        public SpaService(RelayConfiguration config, HttpClient httpClient)
        {
            _config = config;
            _httpClient = httpClient;
        }

        public Stream ServeSpaHtml()
        {
            var htmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "directory-browser.html");

            if (File.Exists(htmlPath))
            {
                var html = File.ReadAllText(htmlPath);
                var bytes = Encoding.UTF8.GetBytes(html);

                var context = WebOperationContext.Current;
                if (context != null)
                {
                    context.OutgoingResponse.StatusCode = HttpStatusCode.OK;
                    context.OutgoingResponse.ContentType = "text/html; charset=utf-8";
                    context.OutgoingResponse.Headers["Cache-Control"] = "no-cache";
                }

                return new MemoryStream(bytes);
            }

            return CreateErrorResponse("SPA file not found", HttpStatusCode.NotFound);
        }

        public async Task<Stream> HandleDirectoryListAsync(string path)
        {
            try
            {
                var targetUrl = string.IsNullOrEmpty(path)
                    ? _config.TargetServiceAddress
                    : new Uri(new Uri(_config.TargetServiceAddress), path).ToString();

                var response = await _httpClient.GetAsync(targetUrl);

                if (response.IsSuccessStatusCode)
                {
                    var html = await response.Content.ReadAsStringAsync();
                    var items = ParseDirectoryListing(html);
                    var json = Newtonsoft.Json.JsonConvert.SerializeObject(new { items });

                    var context = WebOperationContext.Current;
                    if (context != null)
                    {
                        context.OutgoingResponse.StatusCode = HttpStatusCode.OK;
                        context.OutgoingResponse.ContentType = "application/json";
                        context.OutgoingResponse.Headers["Cache-Control"] = "no-cache";
                    }

                    return new MemoryStream(Encoding.UTF8.GetBytes(json));
                }

                return CreateErrorResponse("Directory not found", response.StatusCode);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{_config.RelayName}] Error in directory API: {ex.Message}");
                return CreateErrorResponse("Internal server error", HttpStatusCode.InternalServerError);
            }
        }

        public async Task<Stream> HandleFileDownloadAsync(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path))
                {
                    return CreateErrorResponse("File path required", HttpStatusCode.BadRequest);
                }

                var targetUrl = new Uri(new Uri(_config.TargetServiceAddress), path).ToString();
                var response = await _httpClient.GetAsync(targetUrl);

                var context = WebOperationContext.Current;
                if (context != null)
                {
                    context.OutgoingResponse.StatusCode = response.StatusCode;

                    foreach (var header in response.Headers)
                    {
                        if (!header.Key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase))
                        {
                            context.OutgoingResponse.Headers.Add(header.Key, string.Join(",", header.Value));
                        }
                    }

                    if (response.Content != null)
                    {
                        foreach (var header in response.Content.Headers)
                        {
                            if (!header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                            {
                                context.OutgoingResponse.Headers.Add(header.Key, string.Join(",", header.Value));
                            }
                        }

                        var responseBytes = await response.Content.ReadAsByteArrayAsync();
                        return new MemoryStream(responseBytes);
                    }
                }

                return new MemoryStream();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{_config.RelayName}] Error in file download: {ex.Message}");
                return CreateErrorResponse("Internal server error", HttpStatusCode.InternalServerError);
            }
        }

        private List<object> ParseDirectoryListing(string html)
        {
            var items = new List<object>();

            try
            {
                // Simple regex-based parsing of HTML directory listing
                var linkPattern = @"<a\s+href=""([^""]+)""\s+class=""[^""]*icon[^""]*icon-([^""]*)""\s+title=""([^""]+)""[^>]*>.*?<span\s+class=""name"">([^<]+)</span>";
                var matches = Regex.Matches(html, linkPattern, RegexOptions.IgnoreCase);

                foreach (Match match in matches)
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

        private Stream CreateErrorResponse(string message, HttpStatusCode statusCode)
        {
            var context = WebOperationContext.Current;
            if (context != null)
            {
                context.OutgoingResponse.StatusCode = statusCode;
                context.OutgoingResponse.ContentType = "application/json";
            }

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(new { error = message });
            return new MemoryStream(Encoding.UTF8.GetBytes(json));
        }
    }
}
