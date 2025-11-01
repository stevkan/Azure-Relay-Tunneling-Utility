using System;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Relay;

namespace RelayTunnelUsingHybridConnection
{
    internal class WebSocketForwarder
    {
        private readonly Uri _targetWebSocketAddress;
        
        public WebSocketForwarder(Uri targetWebSocketAddress)
        {
            _targetWebSocketAddress = targetWebSocketAddress;
        }

        public async Task ForwardAsync(HybridConnectionStream relayStream, Uri targetUri, CancellationToken ct)
        {
            using var ws = new ClientWebSocket();

            // Optional but helpful for keeping the connection alive (DirectLine Speech often streams continuously)
            ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);

            try
            {
                await ws.ConnectAsync(targetUri, ct);
                Console.WriteLine($"WebSocket connected to target: {targetUri}");

                var pump1 = PumpWebSocketToStreamAsync(ws, relayStream, ct);
                var pump2 = PumpStreamToWebSocketAsync(relayStream, ws, ct);

                // Wait for either direction to finish (close or error)
                await Task.WhenAny(pump1, pump2);

                Console.WriteLine("WebSocket forwarding completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WebSocket forwarding error: {ex.Message}");
            }
            finally
            {
                // Try to close both gracefully
                try { await ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Relay closing", ct); } catch { }
                try { await relayStream.ShutdownAsync(ct); } catch { }
                relayStream.Dispose();
            }
        }

        public async Task BridgeAsync(WebSocket inbound, Uri targetUri, string requestedProtocolsHeader, CancellationToken ct)
        {
            using var outbound = new ClientWebSocket();
            
            // Optional but helpful for keeping the connection alive (DirectLine Speech often streams continuously)
            outbound.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);

            // Forward subprotocols if present so the target can negotiate the same one
            if (!string.IsNullOrEmpty(requestedProtocolsHeader))
            {
                var protocols = requestedProtocolsHeader.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var p in protocols) 
                {
                    outbound.Options.AddSubProtocol(p);
                }
            }

            await outbound.ConnectAsync(targetUri, ct);
            Console.WriteLine($"WebSocket connected to target: {targetUri}");

            var pump1 = PumpAsync(inbound, outbound, "Inbound->Target", ct);
            var pump2 = PumpAsync(outbound, inbound, "Target->Inbound", ct);

            // Wait for either direction to finish (close or error)
            await Task.WhenAny(pump1, pump2);

            // Try to close both gracefully
            await SafeCloseAsync(inbound, "Relay closing", ct);
            await SafeCloseAsync(outbound, "Relay closing", ct);
        }

        private static async Task PumpWebSocketToStreamAsync(ClientWebSocket ws, Stream stream, CancellationToken ct)
        {
            var buffer = new byte[8192];
            try
            {
                while (ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
                {
                    var result = await ws.ReceiveAsync(buffer, ct);
                    if (result.MessageType == WebSocketMessageType.Close) break;
                    if (result.Count > 0)
                    {
                        await stream.WriteAsync(buffer, 0, result.Count, ct);
                        await stream.FlushAsync(ct);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WebSocket->Stream pump error: {ex.Message}");
            }
        }

        private static async Task PumpStreamToWebSocketAsync(Stream stream, ClientWebSocket ws, CancellationToken ct)
        {
            var buffer = new byte[8192];
            try
            {
                while (ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
                {
                    var read = await stream.ReadAsync(buffer, 0, buffer.Length, ct);
                    if (read <= 0) break;
                    // We don't know original message boundaries; we send each chunk as a complete frame.
                    await ws.SendAsync(new ArraySegment<byte>(buffer, 0, read), WebSocketMessageType.Binary, endOfMessage: true, cancellationToken: ct);
                }
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Stream->WebSocket pump error: {ex.Message}");
            }
        }

        private static async Task PumpAsync(WebSocket source, WebSocket destination, string name, CancellationToken ct)
        {
            var buffer = new byte[8192];

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var result = await source.ReceiveAsync(new ArraySegment<byte>(buffer), ct);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        // Echo the close to the other side
                        await destination.CloseOutputAsync(source.CloseStatus ?? WebSocketCloseStatus.NormalClosure,
                            source.CloseStatusDescription ?? "Closing", ct);
                        break;
                    }

                    // Preserve message type and EndOfMessage
                    await destination.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count),
                        result.MessageType,
                        result.EndOfMessage,
                        ct);
                }
            }
            catch (OperationCanceledException) 
            { 
                // Normal shutdown
                Console.WriteLine($"{name} pump cancelled");
            }
            catch (WebSocketException wse)
            {
                Console.WriteLine($"{name} WebSocketException: {wse.Message}");
                try 
                { 
                    await destination.CloseOutputAsync(WebSocketCloseStatus.InternalServerError, "Proxy error", CancellationToken.None); 
                } 
                catch 
                { 
                    // Ignore close errors 
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{name} error: {ex.Message}");
                try 
                { 
                    await destination.CloseOutputAsync(WebSocketCloseStatus.InternalServerError, "Proxy error", CancellationToken.None); 
                } 
                catch 
                { 
                    // Ignore close errors
                }
            }
        }

        private static async Task SafeCloseAsync(WebSocket ws, string reason, CancellationToken ct)
        {
            try
            {
                if (ws.State == WebSocketState.Open || ws.State == WebSocketState.CloseReceived)
                {
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, reason, ct);
                }
            }
            catch 
            { 
                // Ignore close errors
            }
        }
    }
}
