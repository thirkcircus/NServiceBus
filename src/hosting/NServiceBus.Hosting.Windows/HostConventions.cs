using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using NServiceBus.Hosting.Helpers;
using NServiceBus.Hosting.Windows.Arguments;

namespace NServiceBus.Hosting.Windows
{
    /// <summary>
    /// The set of conventions used to setup the host which allow the users to customize
    /// the way the Host behaves
    /// </summary>
    public static class HostConventions
    {
        static IEnumerable<Assembly> cachedAssemblies;
        static IEnumerable<EndpointDefinition> cachedEndpointDefinitions;
        static IEnumerable<Type> cachedEndpointTypes;
        static string cachedServiceName;
        static string cachedDisplayName;

        static HostConventions()
        {
            RevertToDefaultConventions();
        }

        /// <summary>
        /// Reverts all conventions to their default
        /// </summary>
        public static void RevertToDefaultConventions()
        {
            GetHostArguments = () => null;
            GetScannableAssemblies = () => cachedAssemblies ?? (cachedAssemblies = AssemblyScanner.GetScannableAssemblies().Assemblies);
            
            GetServiceName = () => cachedServiceName ?? (cachedServiceName = DefaultGetServiceName());
            GetServiceDisplayName = () => cachedDisplayName ?? (cachedDisplayName = DefaultGetServiceDisplayName());
            GetServiceVersion = DefaultGetServiceVersion;

            GetEndpointDefinitions = () => cachedEndpointDefinitions ?? (cachedEndpointDefinitions = DefaultGetEndpointDefinitions().ToArray());
            CreateEndpointDefinitionFromConfigutationType = DefaultCreateEndpointDefinitionFromConfigutationType;
            GetEndpointTypes = () => cachedEndpointTypes ?? (cachedEndpointTypes = DefaultGetEndpointTypes());
            GetEndpointName = DefaultGetEndpointName;
            GetEndpointConfigurationFile = DefaultGetEndpointConfigurationFile;
            GetEndpointArguments = DefaultGetEndpointArguments;
        }

        /// <summary>
        /// Gets the arguments used to configure the host
        /// </summary>
        public static Func<HostArguments> GetHostArguments { get; set; }

        /// <summary>
        /// Gets the assemblies to use when scanning for types
        /// </summary>
        public static Func<IEnumerable<Assembly>> GetScannableAssemblies { get; set; }
        
        /// <summary>
        /// Convention to get the name of the host service
        /// </summary>
        public static Func<string> GetServiceName { get; set; }

        /// <summary>
        /// Convention to get the display name of the host service
        /// </summary>
        public static Func<string> GetServiceDisplayName { get; set; }

        /// <summary>
        /// Convention to get the version of an endpoint given its type
        /// </summary>
        public static Func<string> GetServiceVersion { get; set; }

        /// <summary>
        /// Gets the convention to use when selecting the endpoints to host
        /// </summary>
        public static Func<IEnumerable<EndpointDefinition>> GetEndpointDefinitions { get; set; }

        /// <summary>
        /// Gets the convention to create an endpoint definition given an endpoint configuration type
        /// </summary>
        public static Func<Type, EndpointDefinition> CreateEndpointDefinitionFromConfigutationType { get; set; }
        
        /// <summary>
        /// The convention to use when selecting the types for the endpoints to host
        /// </summary>
        public static Func<IEnumerable<Type>> GetEndpointTypes { get; set; }
        
        /// <summary>
        /// Convention to get the name of an endpoint given its type
        /// </summary>
        public static Func<Type, string> GetEndpointName { get; set; }
        
        /// <summary>
        /// Convention to get the configuration file of an endpoint given its type
        /// </summary>
        public static Func<Type, string> GetEndpointConfigurationFile { get; set; }

        /// <summary>
        /// Gets the command line arguments to use when starting up an endpoint process
        /// </summary>
        public static Func<EndpointDefinition, HostArguments> GetEndpointArguments { get; set; }

        /// <summary>
        /// Default convention for getting the name of the host service
        /// </summary>
        /// <returns>The service name</returns>
        public static string DefaultGetServiceName()
        {
            var arguments = GetHostArguments();

            if (arguments.ServiceName != null)
                return arguments.ServiceName.Value;

            var endpoint = GetEndpointDefinitions().FirstOrDefault();

            if (endpoint == null)
                throw new InvalidOperationException("Cannot define the service name for the host. No endpoint is defined & a service name argument was not provided");

            var serviceName = endpoint.Name;

            if (arguments.SideBySide != null)
            {
                var version = GetServiceVersion();
                serviceName += "-" + version;
            }

            return serviceName;
        }

        /// <summary>
        /// Default convention for getting the display name of the host service
        /// </summary>
        /// <returns>The display name</returns>
        public static string DefaultGetServiceDisplayName()
        {
            var endpoint = GetEndpointDefinitions()
                .FirstOrDefault();

            if (endpoint == null)
                throw new InvalidOperationException("Cannot define the service name for the host. No endpoint is defined & a service name argument was not provided");

            var serviceName = GetServiceName();

            var displayName = serviceName + "-" + GetServiceVersion();

            var arguments = GetHostArguments();

            if (arguments.SideBySide != null)
            {
                displayName += " (SideBySide)";
            }

            return displayName;
        }

        /// <summary>
        /// Default convention for getting the version of an endpoint
        /// </summary>
        /// <param name="endpointConfigurationType"></param>
        /// <returns>The version of the endpoint</returns>
        public static string DefaultGetServiceVersion()
        {
            var endpoint = GetEndpointDefinitions().FirstOrDefault();

            if (endpoint == null)
                throw new InvalidOperationException("Cannot get version for host service. Atleast one endpoint must be defined");

            var fileVersion = FileVersionInfo.GetVersionInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, endpoint.ConfigurationType.Assembly.ManifestModule.Name));

            //build a semver compliant version
            return String.Format("{0}.{1}.{2}", fileVersion.FileMajorPart, fileVersion.FileMinorPart, fileVersion.FileBuildPart);
        }
        
        /// <summary>
        /// The default convention for getting the endpoint definitions to host
        /// </summary>
        /// <returns>An enumerable of endpoints to host</returns>
        public static IEnumerable<EndpointDefinition> DefaultGetEndpointDefinitions()
        {
            var endpointTypes = GetEndpointTypes().ToArray();

            if (endpointTypes != null && endpointTypes.Count() > 1)
            {
                throw new InvalidOperationException("Host doesn't support hosting of multiple endpoints. " +
                                                    "Endpoint classes found: " +
                                                    String.Join(", ", endpointTypes.Select(e => e.AssemblyQualifiedName).ToArray()) +
                                                    " You may have some old assemblies in your runtime directory." +
                                                    " Try right-clicking your VS project, and selecting 'Clean'. " +
                                                    "If you want to run multiple endpoints in a single host, you can run in multihost mode using the '/multihost' command-line argument"
                    );

            }

            return endpointTypes.Select(DefaultCreateEndpointDefinitionFromConfigutationType).ToArray();
        }

        /// <summary>
        /// Default convention for getting the endpoint configuration types to host
        /// </summary>
        /// <returns>An enumerable of endpoint types.</returns>
        public static IEnumerable<Type> DefaultGetEndpointTypes()
        {
            var arguments = GetHostArguments();

            if (arguments.EndpointConfigurationType != null)
            {
                string t = arguments.EndpointConfigurationType.Value;
                if (t != null)
                {
                    Type endpointType = Type.GetType(t, false);
                    if (endpointType == null)
                        throw new ConfigurationErrorsException(String.Format("Command line argument 'endpointConfigurationType' has specified to use the type '{0}' but that type could not be loaded.", t));

                    return new[] { endpointType };
                }
            }

            var endpoint = ConfigurationManager.AppSettings["EndpointConfigurationType"];
            if (endpoint != null)
            {
                var endpointType = Type.GetType(endpoint, false);
                if (endpointType == null)
                    throw new ConfigurationErrorsException(String.Format("The 'EndpointConfigurationType' entry in the NServiceBus.Host.exe.config has specified to use the type '{0}' but that type could not be loaded.", endpoint));

                return new[] { endpointType };
            }


            var scannableAssemblies = GetScannableAssemblies();

            var configurationTypes = scannableAssemblies
                .AllTypesAssignableTo<IConfigureThisEndpoint>()
                .Where(t => !t.IsAbstract).ToArray();

            return configurationTypes.ToArray();
        }

        /// <summary>
        /// The default convention for creating endpoint definitions from an endpointType
        /// </summary>
        /// <param name="endpointType"></param>
        /// <returns></returns>
        public static EndpointDefinition DefaultCreateEndpointDefinitionFromConfigutationType(Type endpointType)
        {
            var constructor = endpointType.GetConstructor(Type.EmptyTypes);

            if (constructor == null)
                throw new InvalidOperationException("Endpoint configuration type needs to have a default constructor: " +
                                                    endpointType.FullName);

            var name = GetEndpointName(endpointType);
            var configFile = GetEndpointConfigurationFile(endpointType);

            var endpoint = new EndpointDefinition(endpointType, name, configFile);
            return endpoint;
        }
        
        /// <summary>
        /// Default convention for getting the endpoint name, which serves to identify the endpoint.
        /// </summary>
        /// <param name="endpointConfigurationType"></param>
        /// <returns>The name of the endpoint</returns>
        public static string DefaultGetEndpointName(Type endpointConfigurationType)
        {
            var endpointConfiguration = Activator.CreateInstance(endpointConfigurationType);
            var endpointName = endpointConfiguration.GetType().Namespace;

            var arguments = GetHostArguments();

            if (arguments.ServiceName != null)
                endpointName = arguments.ServiceName.Value;

            var arr = endpointConfiguration.GetType().GetCustomAttributes(typeof(EndpointNameAttribute), false);
            if (arr.Length == 1)
                endpointName = (arr[0] as EndpointNameAttribute).Name;

            if (endpointConfiguration is INameThisEndpoint)
                endpointName = (endpointConfiguration as INameThisEndpoint).GetName();

            if (arguments.EndpointName != null)
                endpointName = arguments.EndpointName.Value;

            return endpointName;
        }

        /// <summary>
        /// Default convention for getting the application configuration file (app.config) for an endpoint
        /// </summary>
        /// <param name="endpointConfigurationType"></param>
        /// <returns>The location of the configuration file for the endpoint</returns>
        public static string DefaultGetEndpointConfigurationFile(Type endpointConfigurationType)
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, endpointConfigurationType.Assembly.ManifestModule.Name + ".config");
        }

        /// <summary>
        /// Default convention gor getting the command line arguments to use when starting up an endpoint process
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        public static HostArguments DefaultGetEndpointArguments(EndpointDefinition endpoint)
        {
            var arguments = GetHostArguments();
            IEnumerable<string> args = arguments.AsCommandLineArguments();

            //Add the endpoint name so that the new appdomain can get it
            if (arguments.EndpointName == null)
                args = args.Concat(new[] { "/endpointName:" + endpoint.Name });

            //Add the ScannedAssemblies name so that the new appdomain can get it
            if (arguments.ScannedAssemblies == null)
            {
                var assemblies = GetScannableAssemblies().Select(s => s.ToString()).ToArray();
                args = args.Concat(new[] { "/scannedassemblies:" + String.Join(";", assemblies) });
            }

            //Add the endpointConfigurationType name so that the new appdomain can get it
            if (arguments.EndpointConfigurationType == null)
                args = args.Concat(new[] { "/endpointConfigurationType:" + endpoint.ConfigurationType.AssemblyQualifiedName });

            return new HostArguments(args.ToArray());
        }
    }
}