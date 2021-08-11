using System.Threading.Tasks;
using UnityEngine;

namespace Unity.Services.Core
{
    /// <summary>
    /// Utility to initialize all Unity services from a single endpoint.
    /// </summary>
    public static class UnityServices
    {
        internal static IUnityServices Instance { get; set; }

        /// <summary>
        /// Initialization state.
        /// </summary>
        public static ServicesInitializationState State => Instance?.State ?? ServicesInitializationState.Uninitialized;

        /// <summary>
        /// Single entry point to initialize all used services.
        /// </summary>
        /// <returns>
        /// Return a handle to the initialization operation.
        /// </returns>
        public static Task Initialize()
        {
            return Initialize(new InitializationOptions());
        }

        /// <summary>
        /// Single entry point to initialize all used services.
        /// </summary>
        /// <param name="options">
        /// The options to customize services initialization.
        /// </param>
        /// <returns>
        /// Return a handle to the initialization operation.
        /// </returns>
        public static Task Initialize(InitializationOptions options)
        {
            if (!Application.isPlaying)
            {
                return Task.FromException(
                    new ServicesInitializationException("You are attempting to initialize Unity Services in Edit Mode." +
                        " Unity Services can only be initialized in Play Mode"));
            }

            if (Instance == null)
            {
                return Task.FromException(
                    new ServicesInitializationException("You are attempting to initialize Unity Services too early." +
                        " Please consider to move your initialization logic to happen after RuntimeInitializeLoadType.AfterAssembliesLoaded"));
            }

            return Instance.Initialize(options);
        }
    }
}
