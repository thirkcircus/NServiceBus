using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NServiceBus.Hosting.Windows.MultiHost
{
    /// <summary>
    /// The set of conventions used to setup the host to host multiple endpoints. These conventions will be
    /// configured if the '/multihost' command-line argument is specified.
    /// </summary>
    public class MultiHostConventions : IWantCustomHostInitialization
    {
        /// <summary>
        /// Will setup host conventions for hosting multiple endpoints if the '/multihost' command-line 
        /// argument is specified.
        /// </summary>
        public void Initialize()
        {
            var arguments = HostConventions.GetHostArguments();

            if (arguments.GetCustomArgument("multihost") == null)
                return;

            SetMultihostConventions();
        }

        /// <summary>
        /// Configures <see cref="HostConventions"/> to use multihost conventions
        /// </summary>
        public static void SetMultihostConventions()
        {
            HostConventions.GetEndpointDefinitions = GetEndpointDefinitions;
            HostConventions.GetEndpointName = GetEndpointName;
            HostConventions.GetEndpointConfigurationFile = DefaultGetEndpointConfigurationFile;
            HostConventions.GetServiceName = GetServiceName;
        }

        /// <summary>
        /// Multihost convention for getting the endpoint definitions to host. Will scan for all types that
        /// implement <see cref="IConfigureThisEndpoint"/>. To limit the endpoints to host, specify the 
        /// '/Endpoints' command-line argument with the endpoint names of the endpoints to run. 
        /// </summary>
        /// <returns>An enumerable of endpoints to host</returns>
        public static IEnumerable<EndpointDefinition> GetEndpointDefinitions()
        {
            var endpointTypes = HostConventions.GetEndpointTypes().ToArray();

            foreach (var endpointType in endpointTypes)
            {
                var constructor = endpointType.GetConstructor(Type.EmptyTypes);

                if (constructor == null)
                    throw new InvalidOperationException("Endpoint configuration type needs to have a default constructor: " + endpointType.FullName);

                var name = HostConventions.GetEndpointName(endpointType);
                var configFile = HostConventions.GetEndpointConfigurationFile(endpointType);

                yield return new EndpointDefinition(endpointType, name, configFile);
            }
        }

        /// <summary>
        /// Multihost convention for getting the endpoint name, which serves to identify the endpoint.
        /// </summary>
        /// <param name="endpointConfigurationType"></param>
        /// <returns>The name of the endpoint</returns>
        public static string GetEndpointName(Type endpointConfigurationType)
        {
            var endpointConfiguration = Activator.CreateInstance(endpointConfigurationType);
            var endpointName = endpointConfiguration.GetType().Namespace;

            var arr = endpointConfiguration.GetType().GetCustomAttributes(typeof(EndpointNameAttribute), false);

            if (arr.Length == 1)
                endpointName = (arr[0] as EndpointNameAttribute).Name;

            if (endpointConfiguration is INameThisEndpoint)
                endpointName = (endpointConfiguration as INameThisEndpoint).GetName();

            return endpointName;
        }

        /// <summary>
        /// Multihost convention for getting the application configuration file (app.config) for 
        /// an endpoint. Tries to find a config file named 'app.config' in a folder that matches 
        /// the endpoint configuration type's namespace relative to the root namespace of the 
        /// assembly, then attempts to find a config file with the same name as the endpoint name 
        /// at the root application directory.
        /// </summary>
        /// <example>
        /// If the root namespace is "My.Handlers" and the endpoint configuration type's namespace 
        /// is My.Handlers.EndpointA, then the convention will look for a configuration file at 
        /// /My.Handlers.EndpointA.config or /EndpointA/App.config
        /// </example>
        /// <param name="endpointConfigurationType"></param>
        /// <returns>The location of the configuration file for the endpoint</returns>
        public static string DefaultGetEndpointConfigurationFile(Type endpointConfigurationType)
        {
            string configFile;

            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var endpointNamespace = endpointConfigurationType.Namespace;
            var rootNamespace = endpointConfigurationType.Assembly.ManifestModule.Name;

            if (!string.IsNullOrEmpty(endpointNamespace) && endpointNamespace.StartsWith(rootNamespace))
            {
                var relativeNamespace = endpointNamespace.Replace(rootNamespace, string.Empty);
                var relativeConfigFile = Path.Combine(relativeNamespace, "app.config");
                configFile = Path.Combine(baseDirectory, relativeConfigFile);

                if (File.Exists(configFile))
                    return configFile;
            }

            var endpointName = HostConventions.GetEndpointName(endpointConfigurationType);
            configFile = Path.Combine(baseDirectory, endpointName + ".config");

            return configFile;
        }

        /// <summary>
        /// Default convention for getting the name of the host service
        /// </summary>
        /// <returns>The service name</returns>
        public static string GetServiceName()
        {
            var arguments = HostConventions.GetHostArguments();

            if (arguments.ServiceName == null)
                throw new InvalidOperationException("Cannot define service name for host. A service name must be specified in the command-line arguments when running in multihost mode");

            return arguments.ServiceName.Value;
        }
    }
}
