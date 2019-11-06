using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using WebSocketSharp;
using XDeploy.Core;

namespace XDeploy.Client.Infrastructure
{
    /// <summary>
    /// Represents an implementation of the <see cref="ISyncSignalNotifier"/>, based on a WebSocket connection.
    /// </summary>
    /// <seealso cref="XDeploy.Client.Infrastructure.ISyncSignalNotifier" />
    public class WebSocketsSignalNotifier : ISyncSignalNotifier
    {
        private readonly string _endpoint;
        private readonly string _authString;
        private readonly IList<(string ID, WebSocket WebSocket)> _appWs;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketsSignalNotifier"/> class.
        /// </summary>
        /// <param name="apps">The apps.</param>
        /// <param name="enpoint">The enpoint address.</param>
        /// <param name="username">The username.</param>
        /// <param name="apiKey">The API key.</param>
        /// <exception cref="ArgumentNullException">
        /// apps
        /// or
        /// enpoint
        /// or
        /// username
        /// or
        /// apiKey
        /// </exception>
        public WebSocketsSignalNotifier(IEnumerable<ApplicationInfo> apps, string enpoint, string username, string apiKey)
        {
            if (apps is null)
                throw new ArgumentNullException(nameof(apps));
            if (string.IsNullOrEmpty(enpoint))
                throw new ArgumentNullException(nameof(enpoint));
            if (string.IsNullOrEmpty(username))
                throw new ArgumentNullException(nameof(username));
            if (string.IsNullOrEmpty(apiKey))
                throw new ArgumentNullException(nameof(apiKey));

            _endpoint = enpoint;
            _authString = $"Basic {Cryptography.Base64Encode($"{username}:{apiKey}")}";
            _appWs = apps.Select(x => (x.ID, (WebSocket)null)).ToList();
        }

        /// <summary>
        /// Occurs when the server receives a correct synchronization signal.
        /// </summary>
        public event EventHandler<string> SyncSignalReceived;

        /// <summary>
        /// Starts listening.
        /// </summary>
        public void StartListening()
        {
            for (int i = 0; i < _appWs.Count; i++)
            {
                var ws = new WebSocket(string.Format("{0}/api/ws?authString={1}&id={2}", _endpoint.Replace("https://", "wss://").Replace("http://", "ws://"), _authString.Replace("Basic ", string.Empty), _appWs[i].ID));
                ws.SslConfiguration.CheckCertificateRevocation = false;
                ws.OnMessage += (sender, e) =>
                {
                    SyncSignalReceived?.Invoke(this, ((dynamic)JsonConvert.DeserializeObject(e.Data)).id.ToString());
                };
                ws.Connect();
                _appWs[i] = (_appWs[i].ID, ws);
            }
            Console.WriteLine("Waiting for updates...");
        }

        /// <summary>
        /// Stops listening.
        /// </summary>
        public void StopListening()
        {
            for (int i = 0; i < _appWs.Count; i++)
            {
                _appWs[i].WebSocket.Close(CloseStatusCode.Normal);
                _appWs[i] = (_appWs[i].ID, (WebSocket)null);
            }
        }
    }
}
