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
    [Route("/ws/app-status")]
    [Authorize]
    public class ApplicationStatusWebSocketsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ApplicationStatusWebSocketsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task Get()
        {
            var context = ControllerContext.HttpContext;
            if (context.WebSockets.IsWebSocketRequest)
            {
                WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                await Task.Run(async () =>
                {
                    WebsocketsIOC.RegisterApplicationLockedChanged(async ((string ApplicationID, bool Locked) data) =>
                    {
                        var app = _context.Applications.Find(data.ApplicationID);
                        if (app?.OwnerEmail == User.Identity.Name)
                        {
                            await SendMessageAsync(context, webSocket, JsonConvert.SerializeObject(new { action = "lockedChanged", id = data.ApplicationID, locked = data.Locked }));
                        }
                    });
                    while (webSocket.State == WebSocketState.Open)
                    {
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
