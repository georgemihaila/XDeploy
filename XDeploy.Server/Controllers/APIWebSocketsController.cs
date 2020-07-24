using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using XDeploy.Server.Infrastructure.Data;

namespace XDeploy.Server.Controllers
{
    [Route("/api/ws")]
    [AllowAnonymous]
    public class APIWebSocketsController : APIValidationBase
    {
        public APIWebSocketsController(ApplicationDbContext context) : base(context)
        {

        }

        [HttpGet]
        public async Task Get(string authString, string id)
        {
            var context = ControllerContext.HttpContext;
            if (context.WebSockets.IsWebSocketRequest)
            {
                WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                await Task.Run(async () =>
                {
                    while (webSocket.State == WebSocketState.Open)
                    {
                        //Code
                        if (webSocket.State == WebSocketState.CloseReceived)
                        {
                            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection terminated.", CancellationToken.None);
                            return;
                        }
                        await Task.Delay(1000);
                    }
                    
                });
                
            }
            else
            {
                context.Response.StatusCode = 400;
            }
        }
        private static async Task SendMessageAsync(HttpContext context, WebSocket webSocket, string message)
        {
            var bytes = Encoding.ASCII.GetBytes(message);
            var arraySegment = new ArraySegment<byte>(bytes);
            await webSocket.SendAsync(arraySegment, WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }
}
