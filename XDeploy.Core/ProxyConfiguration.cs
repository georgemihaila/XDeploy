using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.Json.Serialization;

namespace XDeploy.Core
{
    /// <summary>
    /// Represents a proxy configuration.
    /// </summary>
    public class ProxyConfiguration : IWebProxy
    {
        /// <summary>
        /// Gets or sets the address of the proxy.
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// The credentials to submit to the proxy server for authentication.
        /// </summary>
        public NetworkCredential ProxyCredentials { get; set; }

        [JsonIgnore]
        public ICredentials Credentials
        {
            get
            {
                return ProxyCredentials;
            }
            set
            {
                ProxyCredentials = (NetworkCredential)value;
            }
        }

        /// <summary>
        /// Returns the URI of a proxy.
        /// </summary>
        /// <param name="destination">A <see cref="T:System.Uri" /> that specifies the requested Internet resource.</param>
        /// <returns>
        /// A <see cref="T:System.Uri" /> instance that contains the URI of the proxy used to contact <paramref name="destination" />.
        /// </returns>
        /// <exception cref="NotImplementedException"></exception>
        public Uri GetProxy(Uri destination) => new Uri(Address, UriKind.Absolute);

        /// <summary>
        /// Indicates that the proxy should not be used for the specified host.
        /// </summary>
        /// <param name="host">The <see cref="T:System.Uri" /> of the host to check for proxy use.</param>
        /// <returns>
        ///   <see langword="true" /> if the proxy server should not be used for <paramref name="host" />; otherwise, <see langword="false" />.
        /// </returns>
        /// <exception cref="NotImplementedException"></exception>
        public bool IsBypassed(Uri host) => false;
    }
}
