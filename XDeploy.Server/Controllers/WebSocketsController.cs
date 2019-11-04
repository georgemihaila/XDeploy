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
    public class WebSocketsController : APIValidationBase
    {
        public WebSocketsController(ApplicationDbContext context) : base(context)
        {

        }

        [HttpGet]
        public async Task Get(string authString, string id)
        {
            var context = ControllerContext.HttpContext;
            if (context.WebSockets.IsWebSocketRequest)
            {
                var creds = Decode(authString);
                if (!ValidateCredentials(creds))
                {
                    context.Response.StatusCode = 401;
                    return;
                }
                var app = _context.Applications.Find(id);
                if (app is null || app.OwnerEmail != creds.Value.Email)
                {
                    context.Response.StatusCode = 401;
                    return;
                }
                WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                var lastUpdate = app.LastUpdate;
                await Task.Run(async () =>
                {
                    StaticWebSocketsWorkaround.RegisterOnAppUpdate(id, async (data) =>
                    {
                        await SendMessageAsync(context, webSocket, JsonConvert.SerializeObject(new { action = "update", id = data }));
                    });
                    while (webSocket.State == WebSocketState.Open)
                    {
                        await Task.Delay(1000);
                    }
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection terminated.", CancellationToken.None);
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
