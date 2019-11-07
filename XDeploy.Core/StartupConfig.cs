using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using XDeploy.Core.Exceptions;

namespace XDeploy.Core
{

    [JsonConverter(typeof(StringEnumConverter))]
    public enum ApplicationMode { Deployer, Updater }

    /// <summary>
    /// Represents a startup configuration.
    /// </summary>
    public class StartupConfig
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StartupConfig"/> class, based on a json file.
        /// </summary>
        /// <param name="jsonfilename">The json file's name.</param>
        /// <returns></returns>
        /// <exception cref="XDeploy.Core.ConsoleInfrastructure.StartupException">
        /// Configuration file {filename} not found.
        /// or
        /// Malformed configuration file
        /// </exception>
        public static StartupConfig FromJsonFile(string jsonfilename)
        {
            if (!File.Exists(jsonfilename))
            {
                throw new StartupException($"Configuration file {jsonfilename} not found.");
            }
            try
            {
                return JsonConvert.DeserializeObject<StartupConfig>(File.ReadAllText(jsonfilename));
            }
            catch (Exception e)
            {
                throw new StartupException("Malformed configuration file", e);
            }
        }

        /// <summary>
        /// Gets or sets the application mode.
        /// </summary>
        public ApplicationMode Mode { get; set; }

        /// <summary>
        /// Gets or sets the endpoint.
        /// </summary>
        public string Endpoint { get; set; }

        /// <summary>
        /// Gets or sets the email.
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets the API key.
        /// </summary>
        public string APIKey { get; set; }

        /// <summary>
        /// Gets or sets the synchronization server's port.
        /// </summary>
        public int SyncServerPort { get; set; }

        /// <summary>
        /// Gets or sets the apps.
        /// </summary>
        public IEnumerable<ApplicationInfo> Apps { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the application will show detailed execution logs.
        /// </summary>
        public bool Verbose { get; set; }

        /// <summary>
        /// Gets or sets the proxy configuration.
        /// </summary>
        public ProxyConfiguration Proxy { get; set; }
    }
}
