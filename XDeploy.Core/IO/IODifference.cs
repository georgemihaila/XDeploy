using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace XDeploy.Core.IO
{
    /// <summary>
    /// Represents an IO difference.
    /// </summary>
    public class IODifference
    {
        public enum ObjectType { File, Directory }

        public enum IODifferenceType { Removal, Addition, Update }

        /// <summary>
        /// Gets or sets the path.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the type of the object.
        /// </summary>
        public ObjectType Type { get; set; }

        /// <summary>
        /// Gets or sets the difference type.
        /// </summary>
        public IODifferenceType DifferenceType { get; set; }
    }
}
