using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using XDeploy.Core;
using XDeploy.Core.IO;

namespace XDeploy.Client.Infrastructure
{
    /// <summary>
    /// Represents a base class for building an application synchronizer.
    /// </summary>
    /// <seealso cref="XDeploy.Client.Infrastructure.IApplicationSynchronizer" />
    public abstract class ApplicationSynchronizerBase : IApplicationSynchronizer
    {
        protected readonly XDeployAPI _api;
        protected readonly ApplicationInfo _app;
        protected Tree _localTree;
        protected FileManager _fileManager;

        protected const string TimeFormat = "HH:mm:ss";
        protected const string NL = "\r\n";
        protected const string NLT = "\r\n\t";

        protected bool _initial;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationSynchronizerBase"/> class.
        /// </summary>
        /// <param name="api">The API.</param>
        /// <param name="app">The application.</param>
        /// <exception cref="ArgumentNullException">
        /// api
        /// or
        /// app
        /// </exception>
        protected ApplicationSynchronizerBase(XDeployAPI api, ApplicationInfo app)
        {
            if (api is null)
                throw new ArgumentNullException(nameof(api));
            if (app is null)
                throw new ArgumentNullException(nameof(app));

            _api = api;
            _app = app;

            _localTree = new Tree(_app.Location);
            _localTree.Relativize(); //Such that differences are computed correctly
            _initial = true;
            _fileManager = new FileManager(app.Location);
        }

        /// <summary>
        /// Gets the application ID.
        /// </summary>
        public string ApplicationID => _app.ID;

        /// <summary>
        /// Checks for local file changes, compares their versions to the ones on the server and synchronizes them if required.
        /// </summary>
        public abstract Task<SynchronizationResult> SynchronizeAsync();
    }
}
