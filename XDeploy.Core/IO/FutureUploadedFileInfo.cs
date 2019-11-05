using System;
using System.Collections.Generic;
using System.Text;

namespace XDeploy.Core.IO
{
    /// <summary>
    /// Provides basic information about a file that will be uploaded/downloaded.
    /// </summary>
    public class ExpectedFileInfo
    {
        /// <summary>
        /// Gets or sets the filename.
        /// </summary>
        public string Filename { get; set; }

        /// <summary>
        /// Gets or sets the checksum.
        /// </summary>
        public string Checksum { get; set; }

        /// <summary>
        /// Converts to string.
        /// </summary>
        public override string ToString() => Filename;
    }
}
