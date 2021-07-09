using System;
using System.Collections.Generic;

namespace Unity.Services.Core
{
    /// <summary>
    /// A container to store all available <see cref="IInitializablePackage"/>
    /// and <see cref="IServiceComponent"/> in the project.
    /// </summary>
    public sealed class CoreRegistry
    {
        const string k_LockedPackageRegistrationErrorMessage = "Package registration has been blocked. " +
            "Make sure to register service packages in" +
            "[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)].";

        const string k_LockedComponentRegistrationErrorMessage = "Component registration has been blocked. " +
            "Make sure to register service components before all packages have finished initializing.";

        /// <summary>
        /// Get the only registry of this project.
        /// </summary>
        public static CoreRegistry Instance { get; internal set; } = new CoreRegistry();

        /// <summary>
        /// Key: Hash code of a <see cref="IServiceComponent"/> type.
        /// Value: Component instance.
        /// </summary>
        internal readonly Dictionary<int, IServiceComponent> ComponentTypeHashToInstance;

        internal DependencyTree Tree;

        internal bool IsPackageRegistrationLocked { get; private set; }

        internal bool IsComponentRegistrationLocked { get; private set; }

        internal CoreRegistry()
            : this(new DependencyTree()) {}

        internal CoreRegistry(DependencyTree tree)
        {
            Tree = tree;
            ComponentTypeHashToInstance = tree.ComponentTypeHashToInstance;
        }

        /// <summary>
        /// Store the given <paramref name="package"/> in this registry.
        /// </summary>
        /// <param name="package">
        /// The service package instance to register.
        /// </param>
        /// <typeparam name="T">
        /// The type of <see cref="IInitializablePackage"/> to register.
        /// </typeparam>
        /// <returns>
        /// Return a handle to the registered <paramref name="package"/>
        /// to define its dependencies and provided components.
        /// </returns>
        public CoreRegistration RegisterPackage<T>(T package)
            where T : IInitializablePackage
        {
            if (IsPackageRegistrationLocked)
            {
                throw new InvalidOperationException(k_LockedPackageRegistrationErrorMessage);
            }

            var packageTypeHash = typeof(T).GetHashCode();
            Tree.PackageTypeHashToInstance[packageTypeHash] = package;
            Tree.PackageTypeHashToComponentTypeHashDependencies[packageTypeHash] = new List<int>();

            return new CoreRegistration(this, packageTypeHash);
        }

        /// <summary>
        /// Store the given <paramref name="component"/> in this registry.
        /// </summary>
        /// <param name="component">
        /// The component instance to register.
        /// </param>
        /// <typeparam name="T">
        /// The type of <see cref="IServiceComponent"/> to register.
        /// </typeparam>
        public void RegisterServiceComponent<T>(T component)
            where T : IServiceComponent
        {
            if (IsComponentRegistrationLocked)
            {
                throw new InvalidOperationException(k_LockedComponentRegistrationErrorMessage);
            }

            var componentType = typeof(T);

            // This check is to avoid passing the component without specifying the interface type as a generic argument.
            if (component.GetType() == componentType)
            {
                throw new ArgumentException("Interface type of component not specified.");
            }

            var componentTypeHash = componentType.GetHashCode();
            if (IsComponentTypeRegistered(componentTypeHash))
            {
                throw new ArgumentException(
                    $"A component with the type {componentType.FullName} have already been registered.");
            }

            ComponentTypeHashToInstance[componentTypeHash] = component;
        }

        /// <summary>
        /// Get the instance of the given <see cref="IServiceComponent"/> type.
        /// </summary>
        /// <typeparam name="T">
        /// The type of <see cref="IServiceComponent"/> to get.
        /// </typeparam>
        /// <returns>
        /// Return the instance of the given <see cref="IServiceComponent"/> type if it has been registered;
        /// throws an exception otherwise.
        /// </returns>
        /// <exception cref="KeyNotFoundException">
        /// Thrown if the requested type of <typeparamref name="T"/> hasn't been registered yet.
        /// </exception>
        public T GetServiceComponent<T>()
            where T : IServiceComponent
        {
            var componentType = typeof(T);
            if (!ComponentTypeHashToInstance.TryGetValue(componentType.GetHashCode(), out var component)
                || component == MissingComponent.Instance)
            {
                throw new KeyNotFoundException($"There is no component `{componentType.Name}` registered. " +
                    "Are you missing a package?");
            }

            return (T)component;
        }

        internal void RegisterDependency<TComponent>(int packageTypeHash)
            where TComponent : IServiceComponent
        {
            if (IsPackageRegistrationLocked)
            {
                throw new InvalidOperationException(k_LockedPackageRegistrationErrorMessage);
            }

            var componentTypeHash = typeof(TComponent).GetHashCode();
            ComponentTypeHashToInstance[componentTypeHash] = MissingComponent.Instance;

            AddComponentDependencyToPackage(componentTypeHash, packageTypeHash);
        }

        internal void RegisterOptionalDependency<TComponent>(int packageTypeHash)
            where TComponent : IServiceComponent
        {
            if (IsPackageRegistrationLocked)
            {
                throw new InvalidOperationException(k_LockedPackageRegistrationErrorMessage);
            }

            var componentTypeHash = typeof(TComponent).GetHashCode();
            if (!ComponentTypeHashToInstance.ContainsKey(componentTypeHash))
            {
                ComponentTypeHashToInstance[componentTypeHash] = null;
            }

            AddComponentDependencyToPackage(componentTypeHash, packageTypeHash);
        }

        internal void RegisterProvision<TComponent>(int packageTypeHash)
            where TComponent : IServiceComponent
        {
            if (IsPackageRegistrationLocked)
            {
                throw new InvalidOperationException(k_LockedPackageRegistrationErrorMessage);
            }

            var componentTypeHash = typeof(TComponent).GetHashCode();
            Tree.ComponentTypeHashToPackageTypeHash[componentTypeHash] = packageTypeHash;
        }

        internal void LockPackageRegistration()
        {
            IsPackageRegistrationLocked = true;
        }

        internal void LockComponentRegistration()
        {
            IsComponentRegistrationLocked = true;
        }

        void AddComponentDependencyToPackage(int componentTypeHash, int packageTypeHash)
        {
            var dependencyTypeHashs = Tree.PackageTypeHashToComponentTypeHashDependencies[packageTypeHash];
            if (!dependencyTypeHashs.Contains(componentTypeHash))
            {
                dependencyTypeHashs.Add(componentTypeHash);
            }
        }

        bool IsComponentTypeRegistered(int componentTypeHash)
        {
            return ComponentTypeHashToInstance.TryGetValue(componentTypeHash, out var storedComponent)
                && !(storedComponent is null)
                && storedComponent != MissingComponent.Instance;
        }
    }
}
