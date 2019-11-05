using System;
using System.Collections.Generic;
using System.Text;

namespace XDeploy.Client.Infrastructure
{
    /// <summary>
    /// Represents an application synchronization result.
    /// </summary>
    public class SynchronizationResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SynchronizationResult"/> class.
        /// </summary>
        public SynchronizationResult()
        {
            NewFiles = 0;
        }

        /// <summary>
        /// Gets or sets the number of deployed files.
        /// </summary>
        public int NewFiles { get; set; }

        public override string ToString() => $"New: {NewFiles}";
    }
}
