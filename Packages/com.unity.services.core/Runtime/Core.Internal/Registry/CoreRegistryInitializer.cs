using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Unity.Services.Core
{
    /// <summary>
    /// Helper object to initialize all <see cref="IInitializablePackage"/> registered in a <see cref="CoreRegistry"/>.
    /// </summary>
    class CoreRegistryInitializer
    {
        CoreRegistry m_Registry;

        AsyncOperation m_Operation;

        List<int> m_SortedPackageTypeHashes;

        List<Exception> m_PackageInitializationFailureReasons;

        public CoreRegistryInitializer(
            CoreRegistry registry, AsyncOperation operation, List<int> sortedPackageTypeHashes)
        {
            m_Registry = registry;
            m_Operation = operation;
            m_SortedPackageTypeHashes = sortedPackageTypeHashes;
            m_PackageInitializationFailureReasons = null;
        }

        public void InitializeRegistry()
        {
            if (m_SortedPackageTypeHashes.Count <= 0)
            {
                CompleteInitialization();

                return;
            }

            m_PackageInitializationFailureReasons = new List<Exception>(m_SortedPackageTypeHashes.Count);
            InitializePackageAt(0);
        }

        void CompleteInitialization()
        {
            if (m_PackageInitializationFailureReasons is null
                || m_PackageInitializationFailureReasons.Count <= 0)
            {
                m_Operation.Succeed();
                m_Registry.Tree = null;
            }
            else
            {
                const string errorMessage = "Some component couldn't be initialized. " +
                    "Look at inner exceptions to get more information ont how to fix services initialization.";
                var innerException = new AggregateException(m_PackageInitializationFailureReasons);
                var reason = new ServicesInitializationException(errorMessage, innerException);
                m_Operation.Fail(reason);
            }

            m_PackageInitializationFailureReasons = null;
        }

        void InitializePackageAt(int index)
        {
            var package = GetPackageAt(index);

            try
            {
                var initialization = package.Initialize(m_Registry);
                initialization.ContinueWith(TrackFailureAndProceedInitialization,
                    TaskScheduler.FromCurrentSynchronizationContext());
            }
            catch (Exception e)
            {
                m_PackageInitializationFailureReasons.Add(e);
                InitializePackageAt(index + 1);
            }

            void TrackFailureAndProceedInitialization(Task previousInitialization)
            {
                if (previousInitialization.Status == TaskStatus.Faulted)
                {
                    m_PackageInitializationFailureReasons.Add(previousInitialization.Exception);
                }

                index++;

                if (index >= m_SortedPackageTypeHashes.Count)
                {
                    CompleteInitialization();
                }
                else
                {
                    InitializePackageAt(index);
                }
            }
        }

        IInitializablePackage GetPackageAt(int index)
        {
            var packageTypeHash = m_SortedPackageTypeHashes[index];

            return m_Registry.Tree.PackageTypeHashToInstance[packageTypeHash];
        }
    }
}
