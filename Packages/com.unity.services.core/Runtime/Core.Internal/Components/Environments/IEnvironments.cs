namespace Unity.Services.Core.Environments
{
    /// <summary>
    /// Component providing the Unity Service Environment
    /// </summary>
    public interface IEnvironments : IServiceComponent
    {
        /// <summary>
        /// Returns the id of the currently used Unity Service Environment
        /// </summary>
        string Current { get; }
    }
}
