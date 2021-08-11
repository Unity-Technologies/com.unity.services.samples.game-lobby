using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NotNull = JetBrains.Annotations.NotNullAttribute;

namespace Unity.Services.Core.Internal
{
    /// <summary>
    /// Utility to initialize all Unity services from a single endpoint.
    /// </summary>
    class UnityServicesInternal : IUnityServices
    {
        /// <summary>
        /// Initialization state.
        /// </summary>
        public ServicesInitializationState State { get; private set; }

        public InitializationOptions Options { get; private set; }

        internal bool CanInitialize;

        AsyncOperation m_Initialization;

        [NotNull]
        CoreRegistry Registry { get; }

        public UnityServicesInternal(
            [NotNull] CoreRegistry registry)
        {
            Registry = registry;
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
                return m_Initialization.AsTask();
            }

            StartInitialization();

            return m_Initialization.AsTask();
        }

        bool HasRequestedInitialization()
        {
            return !(m_Initialization is null);
        }

        bool HasInitializationFailed()
        {
            return m_Initialization.Status == AsyncOperationStatus.Failed;
        }

        void CreateInitialization()
        {
            m_Initialization = new AsyncOperation();
            m_Initialization.SetInProgress();
            m_Initialization.Completed += OnInitializationCompleted;
        }

        void StartInitialization()
        {
            State = ServicesInitializationState.Initializing;
            var sortedPackageTypeHashes = new List<int>(
                Registry.PackageRegistry.Tree?.PackageTypeHashToInstance.Count ?? 0);

            try
            {
                var sorter = new DependencyTreeInitializeOrderSorter(
                    Registry.PackageRegistry.Tree, sortedPackageTypeHashes);
                sorter.SortRegisteredPackagesIntoTarget();
            }
            catch (Exception reason)
            {
                m_Initialization.Fail(reason);

                return;
            }

            try
            {
                var initializer = new CoreRegistryInitializer(Registry, m_Initialization, sortedPackageTypeHashes);
                initializer.InitializeRegistry();
            }
            catch (Exception reason)
            {
                m_Initialization.Fail(reason);
            }
        }

        void OnInitializationCompleted(IAsyncOperation initialization)
        {
            switch (initialization.Status)
            {
                case AsyncOperationStatus.Succeeded:
                {
                    State = ServicesInitializationState.Initialized;
                    Registry.LockComponentRegistration();

                    break;
                }
                default:
                {
                    State = ServicesInitializationState.Uninitialized;

                    break;
                }
            }
        }

        internal void EnableInitialization()
        {
            CanInitialize = true;

            Registry.LockPackageRegistration();

            if (HasRequestedInitialization())
            {
                StartInitialization();
            }
        }
    }
}
