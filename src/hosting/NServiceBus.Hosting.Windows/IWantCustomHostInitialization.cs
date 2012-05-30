namespace NServiceBus.Hosting.Windows
{
    /// <summary>
    /// If you want to customize the HostConventions or modify the host arguments, implement this interface.
    /// Implementors will be run prior to initializing the host or endpoint processes. Dependency-injection 
    /// is not provided for these types.
    /// </summary>
    public interface IWantCustomHostInitialization
    {
        /// <summary>
        /// Perform initialization logic.
        /// </summary>
        void Initialize();
    }
}