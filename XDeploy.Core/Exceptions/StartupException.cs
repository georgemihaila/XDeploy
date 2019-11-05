using System;
using System.Collections.Generic;
using System.Text;

namespace XDeploy.Core.Exceptions
{
    /// <summary>
    /// The exception that is thrown when an application is unable to start.
    /// </summary>
    public class StartupException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StartupException"/> class.
        /// </summary>
        public StartupException() : base()
        {

        }
#nullable enable
        /// <summary>
        /// Initializes a new instance of the <see cref="StartupException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public StartupException(string? message) : base(message)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StartupException"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (<see langword="Nothing" /> in Visual Basic) if no inner exception is specified.</param>
        public StartupException(string? message, Exception? innerException) : base(message, innerException)
        {

        }
#nullable disable
    }
}
