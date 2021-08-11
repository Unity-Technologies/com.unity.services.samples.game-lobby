using System.Collections.Generic;

namespace Unity.Services.Core.Internal
{
    /// <summary>
    /// Helper object to sort <see cref="IInitializablePackage"/> stored into a
    /// <see cref="DependencyTree"/> in order they can be initialized successfully.
    /// It adapts the Depth-first Search algorithm.
    /// </summary>
    /// <remarks>
    /// Algorithm source: <see href="https://en.wikipedia.org/wiki/Topological_sorting#Depth-first_search"/>
    /// </remarks>
    struct DependencyTreeInitializeOrderSorter
    {
        enum ExplorationMark
        {
            None,
            Viewed,
            Sorted
        }

        public DependencyTree Tree;

        /// <summary>
        /// The collection containing the sorted package type hashes.
        /// </summary>
        public ICollection<int> Target;

        /// <summary>
        /// History to track packages' exploration state.
        /// Key: Hash code of a <see cref="IInitializablePackage"/> type.
        /// Value: Its exploration state.
        /// </summary>
        Dictionary<int, ExplorationMark> m_PackageTypeHashExplorationHistory;

        public DependencyTreeInitializeOrderSorter(DependencyTree tree, ICollection<int> target)
        {
            Tree = tree;
            Target = target;
            m_PackageTypeHashExplorationHistory = null;
        }

        public void SortRegisteredPackagesIntoTarget()
        {
            Target.Clear();

            RemoveUnprovidedOptionalDependenciesFromTree();

            var registeredPackageTypeHashes = GetPackageTypeHashes();
            m_PackageTypeHashExplorationHistory = new Dictionary<int, ExplorationMark>(registeredPackageTypeHashes.Count);

            foreach (var packageTypeHash in registeredPackageTypeHashes)
            {
                SortTreeThrough(packageTypeHash);
            }

            m_PackageTypeHashExplorationHistory = null;
        }

        void RemoveUnprovidedOptionalDependenciesFromTree()
        {
            foreach (var dependencyTypeHashes in Tree.PackageTypeHashToComponentTypeHashDependencies.Values)
            {
                RemoveUnprovidedOptionalDependencies(dependencyTypeHashes);
            }
        }

        void RemoveUnprovidedOptionalDependencies(IList<int> dependencyTypeHashes)
        {
            for (var i = dependencyTypeHashes.Count - 1; i >= 0; i--)
            {
                var dependencyTypeHash = dependencyTypeHashes[i];
                if (IsOptional(dependencyTypeHash)
                    && !IsProvided(dependencyTypeHash))
                {
                    dependencyTypeHashes.RemoveAt(i);
                }
            }
        }

        void SortTreeThrough(int packageTypeHash)
        {
            m_PackageTypeHashExplorationHistory.TryGetValue(packageTypeHash, out var explorationMark);
            switch (explorationMark)
            {
                case ExplorationMark.Viewed:
                    throw new CircularDependencyException();

                case ExplorationMark.Sorted:
                    return;
            }

            MarkPackage(packageTypeHash, ExplorationMark.Viewed);

            var dependencyTypeHashes = GetDependencyTypeHashesFor(packageTypeHash);
            SortTreeThrough(dependencyTypeHashes);

            Target.Add(packageTypeHash);

            MarkPackage(packageTypeHash, ExplorationMark.Sorted);
        }

        void SortTreeThrough(IEnumerable<int> dependencyTypeHashes)
        {
            foreach (var dependency in dependencyTypeHashes)
            {
                var dependencyPackageTypeHash = GetPackageTypeHashFor(dependency);
                SortTreeThrough(dependencyPackageTypeHash);
            }
        }

        void MarkPackage(int packageTypeHash, ExplorationMark mark)
        {
            m_PackageTypeHashExplorationHistory[packageTypeHash] = mark;
        }

        IReadOnlyCollection<int> GetPackageTypeHashes()
            => Tree.PackageTypeHashToInstance.Keys;

        int GetPackageTypeHashFor(int componentTypeHash)
            => Tree.ComponentTypeHashToPackageTypeHash[componentTypeHash];

        IEnumerable<int> GetDependencyTypeHashesFor(int packageTypeHash)
            => Tree.PackageTypeHashToComponentTypeHashDependencies[packageTypeHash];

        bool IsOptional(int componentTypeHash)
        {
            return Tree.ComponentTypeHashToInstance.TryGetValue(componentTypeHash, out var component)
                && component is null;
        }

        bool IsProvided(int componentTypeHash)
        {
            return Tree.ComponentTypeHashToPackageTypeHash.ContainsKey(componentTypeHash);
        }
    }
}
