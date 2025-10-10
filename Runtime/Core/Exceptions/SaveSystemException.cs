using System;

namespace FLFloppa.SaveSystem
{
    /// <summary>
    /// Represents errors that occur within the save system pipeline.
    /// </summary>
    public class SaveSystemException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SaveSystemException"/> class.
        /// </summary>
        public SaveSystemException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SaveSystemException"/> class with the specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public SaveSystemException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SaveSystemException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public SaveSystemException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
