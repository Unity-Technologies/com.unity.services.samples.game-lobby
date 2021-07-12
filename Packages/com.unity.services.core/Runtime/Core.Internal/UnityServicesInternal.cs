using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Unity.Services.Core
{
    /// <summary>
    /// Utility to initialize all Unity services from a single endpoint.
    /// </summary>
    class UnityServicesInternal : IUnityServices
    {
        /// <summary>
        /// Initialization state.
        /// </summary>
        public ServicesInitializationState State { get; internal set; }

        public InitializationOptions Options { get; internal set; }

        internal bool CanInitialize;

        internal AsyncOperation Initialization;

        /// <summary>
        /// Single entry point to initialize all used services.
        /// </summary>
        /// <param name="options">
        /// The options to customize services initialization.
        /// </param>
        /// <returns>
        /// Return a handle to the initialization operation.
        /// </returns>
        public Task Initialize(InitializationOptions options)
        {
            if (!HasRequestedInitialization()
                || HasInitializationFailed())
            {
                Options = options;
                CreateInitialization();
            }

            if (!CanInitialize
                || State != ServicesInitializationState.Uninitialized)
            {
                return Initialization.AsTask();
            }

            StartInitialization();

            return Initialization.AsTask();
        }

        bool HasRequestedInitialization()
        {
            return !(Initialization is null);
        }

        bool HasInitializationFailed()
        {
            return Initialization.Status == AsyncOperationStatus.Failed;
        }

        void CreateInitialization()
        {
            Initialization = new AsyncOperation();
            Initialization.SetInProgress();
            Initialization.Completed += OnInitializationCompleted;
        }

        void StartInitialization()
        {
            State = ServicesInitializationState.Initializing;
            var registry = CoreRegistry.Instance;
            var sortedPackageTypeHashes = new List<int>(registry.Tree.PackageTypeHashToInstance.Count);

            try
            {
                var sorter = new DependencyTreeInitializeOrderSorter(registry.Tree, sortedPackageTypeHashes);
                sorter.SortRegisteredPackagesIntoTarget();
            }
            catch (Exception reason)
            {
                Initialization.Fail(reason);

                return;
            }

            try
            {
                var initializer = new CoreRegistryInitializer(registry, Initialization, sortedPackageTypeHashes);
                initializer.InitializeRegistry();
            }
            catch (Exception reason)
            {
                Initialization.Fail(reason);
            }
        }

        void OnInitializationCompleted(IAsyncOperation initialization)
        {
            switch (initialization.Status)
            {
                case AsyncOperationStatus.Succeeded:
                {
                    State = ServicesInitializationState.Initialized;
                    CoreRegistry.Instance.LockComponentRegistration();

                    break;
                }
                default:
                {
                    State = ServicesInitializationState.Uninitialized;

                    break;
                }
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        internal static void EnableInitialization()
        {
            var instance = (UnityServicesInternal)UnityServices.Instance;

            instance.CanInitialize = true;
            CoreRegistry.Instance.LockPackageRegistration();

            if (instance.HasRequestedInitialization())
            {
                instance.StartInitialization();
            }
        }
    }
}
