namespace Unity.Services.Core.Device
{
    /// <summary>
    /// Component providing a Unity Installation Identifier
    /// </summary>
    public interface IInstallationId : IServiceComponent
    {
        /// <summary>
        /// Returns Unity Installation Identifier
        /// </summary>
        string GetOrCreateIdentifier();
    }
}
