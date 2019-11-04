using System;
using System.Collections.Generic;
using System.Text;

namespace XDeploy.Core.IO
{
    /// <summary>
    /// Provides basic information about a file that will be uploaded.
    /// </summary>
    public class FutureUploadedFileInfo
    {
        /// <summary>
        /// Gets or sets the filename.
        /// </summary>
        public string Filename { get; set; }

        /// <summary>
        /// Gets or sets the checksum.
        /// </summary>
        public string Checksum { get; set; }
    }
}
