using System;
using System.Collections.Generic;
using System.Text;

namespace XDeploy.Core.IO
{
    /// <summary>
    /// Represents an absolute entity that can be relativized.
    /// </summary>
    public interface IRelativizeable
    {
        /// <summary>
        /// Relativizes the current object
        /// </summary>
        public void Relativize(string absolute);
    }
}
