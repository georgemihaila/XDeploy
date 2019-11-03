using System;
using System.Collections.Generic;
using System.Text;
using NHttp;

namespace XDeploy.Deployer
{
    /// <summary>
    /// Represents an Http server which listens for incoming application synchronization requests.
    /// </summary>
    public class SyncSignalServer
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
            _server = new NHttp.HttpServer()
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
        /// Occurs when the server receives a correct synchronization signal.
        /// </summary>
        public event EventHandler<string> SyncSignalReceived;

        /// <summary>
        /// Starts the server.
        /// </summary>
        public void Start()
        {
            _server.Start();
        }

        /// <summary>
        /// Stops the server.
        /// </summary>
        public void Stop()
        {
            _server.Stop();
        }
    }
}
