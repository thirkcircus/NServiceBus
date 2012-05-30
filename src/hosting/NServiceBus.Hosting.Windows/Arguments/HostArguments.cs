using System.Collections.Generic;
using System.Linq;
using Topshelf;
using Topshelf.Internal;
using Topshelf.Internal.ArgumentParsing;

namespace NServiceBus.Hosting.Windows.Arguments
{
    /// <summary>
    /// A strongly typed wrapper for the command-line arguments for the host
    /// </summary>
    public class HostArguments
    {
        readonly string[] args;
        readonly Parser.Args arguments;
        readonly IEnumerable<IArgument> customArguments;
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="args">A collection of string arguments</param>
        public HostArguments(string[] args)
        {
            this.args = args;
            arguments = Parser.ParseArgs(args);
            customArguments = arguments.CustomArguments;

            Help = GetArgument("help") ?? GetArgument("?");
            ServiceName = GetArgument("serviceName");
            DisplayName = GetArgument("displayName");
            Description = GetArgument("description");
            EndpointConfigurationType = GetArgument("endpointConfigurationType");
            DependsOn = GetArgument("dependsOn");
            StartManually = GetArgument("startManually");
            Username = GetArgument("username");
            Password = GetArgument("password");
            SideBySide = GetArgument("sideBySide");
            EndpointName = GetArgument("endpointName");
            InstallInfrastructure = GetArgument("installInfrastructure");
            ScannedAssemblies = GetArgument("scannedAssemblies");
            Install = arguments.Install;
        }

        /// <summary>
        /// Argument that specifies if the host should run in side-by-side mode
        /// </summary>
        public IArgument SideBySide { get; set; }

        /// <summary>
        /// Argument that specifies if the host should display its help documentation
        /// </summary>
        public IArgument Help { get; set; }

        /// <summary>
        /// Argument that specifies the service name to be used for the host service
        /// </summary>
        public IArgument ServiceName { get; set; }

        /// <summary>
        /// Argument that specifies the display name to be used for the host service
        /// </summary>
        public IArgument DisplayName { get; set; }

        /// <summary>
        /// Argument that specifies the description to be used for the host service
        /// </summary>
        public IArgument Description { get; set; }

        /// <summary>
        /// Argument that specifies an endpoint configuration type for the service to host
        /// </summary>
        public IArgument EndpointConfigurationType { get; set; }
        
        /// <summary>
        /// Argument that specifies the dependencies of the host service
        /// </summary>
        public IArgument DependsOn { get; set; }

        /// <summary>
        /// Argument that specifies if the host service should start manually
        /// </summary>
        public IArgument StartManually { get; set; }

        /// <summary>
        /// Argument that specifies the username to be used when running the host service
        /// </summary>
        public IArgument Username { get; set; }

        /// <summary>
        /// Argument that specifies the password to be used when running the host service
        /// </summary>
        public IArgument Password { get; set; }

        /// <summary>
        /// Argument that specifies the endpoint name to be used to uniquely identify the hosted endpoint
        /// </summary>
        public IArgument EndpointName { get; set; }

        /// <summary>
        /// Argument that specifies if the host should run the infrastructure installers
        /// </summary>
        public IArgument InstallInfrastructure { get; set; }
        
        /// <summary>
        /// Argument that specifies the assemblies to be used when scanning for types
        /// </summary>
        public IArgument ScannedAssemblies { get; set; }
        
        /// <summary>
        /// Argument that specifies if the host should install itself as a service
        /// </summary>
        public bool Install { get; set; }
        
        /// <summary>
        /// Gets a custom argument with the provided key
        /// </summary>
        /// <param name="key"></param>
        /// <returns>The argument with the provided key or null</returns>
        public IArgument GetCustomArgument(string key)
        {
            var argument = arguments.CustomArguments
                .Where(x => x.Key != null)
                .SingleOrDefault(x => x.Key.ToUpper() == key.ToUpper());

            return argument;
        }

        /// <summary>
        /// Converts the host arguments into an array of string
        /// </summary>
        /// <returns>An array of argument values</returns>
        public string[] AsCommandLineArguments()
        {
            return args.ToArray();
        }

        /// <summary>
        /// Converts the host arguments to a string that matches what was passed into the commandline
        /// </summary>
        /// <returns></returns>
        public string AsCommandLine()
        {
            return customArguments.AsCommandLine();
        }

        private IArgument GetArgument(string key)
        {
            var argument = GetCustomArgument(key);

            if (argument != null)
            {
                arguments.CustomArguments = arguments.CustomArguments.Except(new[] { argument });
            }

            return argument;
        }
    }
}