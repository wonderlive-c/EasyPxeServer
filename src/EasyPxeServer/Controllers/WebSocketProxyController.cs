using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Net.WebSockets;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace EasyPxeServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WebSocketProxyController(ILogger<WebSocketProxyController> logger) : ControllerBase
    {
        [HttpGet("/websockify")]
        public async Task Get([FromQuery] string host="10.10.10.10", [FromQuery] int port=5901)
        {
            if (!HttpContext.WebSockets.IsWebSocketRequest)
            {
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                await HttpContext.Response.WriteAsync("This endpoint requires a WebSocket connection.");
                return;
            }

            if (string.IsNullOrEmpty(host)
             || port <= 0
             || port > 65535)
            {
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                await HttpContext.Response.WriteAsync("Invalid host or port parameter.");
                return;
            }

            try
            {
                logger.LogInformation("WebSocket proxy request for {Host}:{Port}", host, port);

                // 接受WebSocket连接
                using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();

                // 连接到目标VNC服务器
                using var tcpClient = new TcpClient();
                await tcpClient.ConnectAsync(host, port);
                await using var networkStream = tcpClient.GetStream();

                logger.LogInformation("Connected to VNC server {Host}:{Port}", host, port);

                // 双向转发数据
                var webSocketTask = ForwardFromWebSocketToTcp(webSocket, networkStream);
                var tcpTask       = ForwardFromTcpToWebSocket(tcpClient, networkStream, webSocket);

                // 等待任一连接关闭
                await Task.WhenAny(webSocketTask, tcpTask);

                logger.LogInformation("Proxy connection closed for {Host}:{Port}", host, port);
            }
            catch (SocketException ex)
            {
                logger.LogError(ex, "Failed to connect to VNC server {Host}:{Port}", host, port);
                HttpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await HttpContext.Response.WriteAsync($"Failed to connect to VNC server: {ex.Message}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in WebSocket proxy");
                HttpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await HttpContext.Response.WriteAsync($"Proxy error: {ex.Message}");
            }
        }

        private static async Task ForwardFromWebSocketToTcp(WebSocket webSocket, NetworkStream networkStream)
        {
            var buffer = new byte[8192]; // 增大缓冲区以提高二进制数据传输效率
            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                    break;
                }

                if (result.Count > 0)
                {
                    // 直接转发二进制数据到TCP流，不做任何转换
                    await networkStream.WriteAsync(buffer, 0, result.Count);
                    await networkStream.FlushAsync();
                }
            }
        }

        private async Task ForwardFromTcpToWebSocket(TcpClient tcpClient, NetworkStream networkStream, WebSocket webSocket)
        {
            var buffer = new byte[8192]; // 增大缓冲区以提高二进制数据传输效率
            try
            {
                // 使用带超时的读取，确保不会错过数据且CPU占用合理
                while (tcpClient.Connected
                    && webSocket.State == WebSocketState.Open)
                {
                    // 设置读取超时，避免无限阻塞
                    networkStream.ReadTimeout = 500; // 500毫秒

                    try
                    {
                        // 尝试读取数据，这是一个阻塞操作但有超时
                        var bytesRead = await networkStream.ReadAsync(buffer, 0, buffer.Length, CancellationToken.None);
                        if (bytesRead > 0)
                        {
                            // 成功读取到数据，发送到WebSocket客户端
                            await webSocket.SendAsync(new ArraySegment<byte>(buffer, 0, bytesRead), WebSocketMessageType.Binary, true, CancellationToken.None);
                        }
                    }
                    catch (IOException ex) when (ex.Message.Contains("timed out"))
                    {
                        // 读取超时是正常的，继续循环
                        // 短暂延迟，避免CPU占用过高
                        await Task.Delay(10);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in ForwardFromTcpToWebSocket");
                // 出错时断开连接
                if (webSocket.State == WebSocketState.Open) { await webSocket.CloseAsync(WebSocketCloseStatus.InternalServerError, "Proxy error", CancellationToken.None); }
            }
        }
    }
}