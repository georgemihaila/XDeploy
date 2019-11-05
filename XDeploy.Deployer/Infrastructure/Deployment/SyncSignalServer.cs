using System;
using NHttp;

namespace XDeploy.Client.Infrastructure
{
    /// <summary>
    /// Represents an Http server which listens for incoming application synchronization requests.
    /// </summary>
    internal class SyncSignalServer : ISyncSignalNotifier
    {
        private readonly HttpServer _server;

        /// <summary>
        /// Gets the port.
        /// </summary>
        public int Port { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SyncSignalServer"/> class.
        /// </summary>
        public SyncSignalServer(int port)
        {
            _server = new HttpServer()
            {
                EndPoint = new System.Net.IPEndPoint(new System.Net.IPAddress(new byte[] { 127, 0, 0, 1 }), port)
            };
            Port = _server.EndPoint.Port;
            _server.RequestReceived += (sender, args) => 
            {
                if (args.Request.QueryString["id"] != null)
                {
                    SyncSignalReceived?.Invoke(this, args.Request.QueryString["id"]);
                }
            };
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="SyncSignalServer"/> is listening.
        /// </summary>
        public bool Listening => _server.State == HttpServerState.Started;

        /// <summary>
        /// Occurs when the server receives a correct synchronization signal.
        /// </summary>
        public event EventHandler<string> SyncSignalReceived;

        /// <summary>
        /// Starts listening.
        /// </summary>
        public void StartListening() => _server.Start();

        /// <summary>
        /// Stops listening.
        /// </summary>
        public void StopListening() => _server.Stop();
    }
}
