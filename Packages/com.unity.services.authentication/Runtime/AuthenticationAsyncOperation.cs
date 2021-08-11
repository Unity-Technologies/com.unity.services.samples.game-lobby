using System;
using System.Collections;
using Unity.Services.Core.Internal;
using AsyncOperation = Unity.Services.Core.Internal.AsyncOperation;
using Logger = Unity.Services.Authentication.Utilities.Logger;

namespace Unity.Services.Authentication
{
    class AuthenticationAsyncOperation : IAsyncOperation
    {
        AsyncOperation m_AsyncOperation;
        AuthenticationException m_AuthenticationException;

        public AuthenticationAsyncOperation()
        {
            m_AsyncOperation = new AsyncOperation();
            m_AsyncOperation.SetInProgress();
        }

        /// <summary>
        /// Complete the operation as a failure.
        /// </summary>
        public void Fail(string errorCode, string message = null, Exception innerException = null)
        {
            Fail(new AuthenticationException(errorCode, message, innerException));
        }

        /// <summary>
        /// Complete the operation as a failure with the exception.
        /// </summary>
        /// <remarks>
        /// Exception with type other than <see cref="AuthenticationException"/> are wrapped as
        /// an <see cref="AuthenticationException"/> with error code <code>AuthenticationError.UnknownError</code>.
        /// </remarks>
        public void Fail(Exception innerException)
        {
            if (innerException is AuthenticationException)
            {
                m_AuthenticationException = (AuthenticationException)innerException;
            }
            else
            {
                m_AuthenticationException = new AuthenticationException(AuthenticationError.UnknownError, null, innerException);
            }
            Logger.LogException(m_AuthenticationException);

            BeforeFail?.Invoke(this);
            m_AsyncOperation.Fail(m_AuthenticationException);
        }

        /// <summary>
        /// Complete this operation as a success.
        /// </summary>
        public void Succeed()
        {
            m_AsyncOperation.Succeed();
        }

        /// <summary>
        /// The event to invoke in case of failure right before marking the operation done.
        /// This is a good place to put some cleanup code before sending out the completed callback.
        /// </summary>
        public event Action<AuthenticationAsyncOperation> BeforeFail;

        /// <inheritdoc/>
        public bool IsDone
        {
            get => m_AsyncOperation.IsDone;
        }

        /// <inheritdoc/>
        public AsyncOperationStatus Status
        {
            get => m_AsyncOperation.Status;
        }

        /// <inheritdoc/>
        public event Action<IAsyncOperation> Completed
        {
            add => m_AsyncOperation.Completed += value;
            remove => m_AsyncOperation.Completed -= value;
        }

        /// <summary>
        /// The exception that occured during the operation if it failed.
        /// The value can be set before the operation is done.
        /// </summary>
        public AuthenticationException Exception
        {
            get => m_AuthenticationException;
        }

        /// <inheritdoc/>
        Exception IAsyncOperation.Exception
        {
            get => m_AuthenticationException;
        }

        /// <inheritdoc/>
        bool IEnumerator.MoveNext() => !IsDone;

        /// <inheritdoc/>
        /// <remarks>
        /// Left empty because we don't support operation reset.
        /// </remarks>
        void IEnumerator.Reset() {}

        /// <inheritdoc/>
        object IEnumerator.Current => null;
    }
}
