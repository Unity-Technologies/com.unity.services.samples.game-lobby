using System;

namespace Unity.Services.Authentication
{
    /// <summary>
    /// AuthenticationException represents a runtime exception from authentication.
    /// </summary>
    public class AuthenticationException : Exception
    {
        /// <summary>
        /// The error code is the identifier for the type of error to handle.
        /// Checkout <c>AuthenticationError</c> for error codes.
        /// </summary>
        public string ErrorCode { get; private set; }

        /// <summary>
        /// Constructor of the AuthenticationException with error code.
        /// </summary>
        /// <param name="errorCode">The error code for AuthenticationException.</param>
        public AuthenticationException(string errorCode)
            : base(errorCode)
        {
            ErrorCode = errorCode;
        }

        /// <summary>
        /// Constructor of the AuthenticationException with error code and a message.
        /// </summary>
        /// <param name="errorCode">The error code for AuthenticationException.</param>
        /// <param name="message">The additional message that helps to debug.</param>
        public AuthenticationException(string errorCode, string message)
            : base(errorCode + ": " + message)
        {
            ErrorCode = errorCode;
        }

        /// <summary>
        /// Constructor of the AuthenticationException with error code, a message and inner exception.
        /// </summary>
        /// <param name="errorCode">The error code for AuthenticationException.</param>
        /// <param name="message">The additional message that helps to debug.</param>
        /// <param name="innerException">The cause of the exception.</param>
        public AuthenticationException(string errorCode, string message, Exception innerException)
            : base(errorCode + ": " + message, innerException)
        {
            ErrorCode = errorCode;
        }
    }
}
