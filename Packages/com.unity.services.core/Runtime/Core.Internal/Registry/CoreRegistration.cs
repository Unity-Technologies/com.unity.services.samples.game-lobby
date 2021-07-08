namespace Unity.Services.Core
{
    /// <summary>
    /// Handle to a registered <see cref="IInitializablePackage"/>.
    /// Provides a fluent interface to define its dependencies and provided components and services.
    /// </summary>
    public readonly struct CoreRegistration
    {
        /// <summary>
        /// The registry to register additional dependencies and provisions.
        /// </summary>
        readonly CoreRegistry m_Registry;

        /// <summary>
        /// The hash of the type of the handled <see cref="IInitializablePackage"/>.
        /// </summary>
        readonly int m_PackageHash;

        internal CoreRegistration(CoreRegistry registry, int packageHash)
        {
            m_Registry = registry;

            m_PackageHash = packageHash;
        }

        /// <summary>
        /// Declare the given component type a dependency of the handled package.
        /// </summary>
        /// <typeparam name="T">
        /// The type of <see cref="IServiceComponent"/> to declare as a dependency for the handled package.
        /// </typeparam>
        /// <returns>
        /// Return this registration.
        /// </returns>
        public CoreRegistration DependsOn<T>()
            where T : IServiceComponent
        {
            m_Registry.RegisterDependency<T>(m_PackageHash);

            return this;
        }

        /// <summary>
        /// Declare the given component type an optional dependency of the handled package.
        /// </summary>
        /// <typeparam name="T">
        /// The type of <see cref="IServiceComponent"/> to declare as an optional dependency for the handled package.
        /// </typeparam>
        /// <returns>
        /// Return this registration.
        /// </returns>
        public CoreRegistration OptionallyDependsOn<T>()
            where T : IServiceComponent
        {
            m_Registry.RegisterOptionalDependency<T>(m_PackageHash);

            return this;
        }

        /// <summary>
        /// Declare the given component type a provided component by the handled package.
        /// </summary>
        /// <typeparam name="T">
        /// The type of <see cref="IServiceComponent"/> to declare provided by the handled package.
        /// </typeparam>
        /// <returns>
        /// Return this registration.
        /// </returns>
        public CoreRegistration ProvidesComponent<T>()
            where T : IServiceComponent
        {
            m_Registry.RegisterProvision<T>(m_PackageHash);

            return this;
        }
    }
}
