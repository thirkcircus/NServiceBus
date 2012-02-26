using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NServiceBus.Hosting.Helpers;
using NServiceBus.Hosting.Windows.Arguments;
using NServiceBus.Hosting.Windows.Conventions;
using Topshelf;
using Topshelf.Configuration;
using System.Configuration;

namespace NServiceBus.Hosting.Windows
{
    using System.Diagnostics;
    using System.Security.Principal;
    using Installers;

    /// <summary>
    /// Entry point to the process.
    /// </summary>
    public class Program
    {
        static void Main(string[] args)
        {
            var arguments = new HostArguments(args);

            if (arguments.Help != null)
            {
                DisplayHelpContent();

                return;
            }

            var hostDefinition = DefineSingleEndpointHost(arguments);
            
            if (hostDefinition == null)
            {
                if (arguments.InstallInfrastructure != null)
                {
                    Console.WriteLine("Running infrastructure installers and exiting (ignoring other command line parameters if exist).");
                    InstallInfrastructure();

                    return;
                }

                throw new InvalidOperationException("No endpoint configuration found in scanned assemblies. " +
                            "This usually happens when NServiceBus fails to load your assembly containing IConfigureThisEndpoint." +
                            " Try specifying the type explicitly in the NServiceBus.Host.exe.config using the appsetting key: EndpointConfigurationType, " +
                            "Scanned path: " + AppDomain.CurrentDomain.BaseDirectory);
            }

            if (arguments.ServiceName != null)
                hostDefinition.ServiceName = arguments.ServiceName.Value;

            if (arguments.SideBySide != null)
            {
                hostDefinition.ServiceName += "-" + hostDefinition.Version;

                hostDefinition.DisplayName += " (SideBySide)";
            }

            if ((arguments.Install) || (arguments.InstallInfrastructure != null))
            {
                AppDomain.CurrentDomain.SetupInformation.AppDomainInitializerArguments = arguments.CommandLineArgs;
                RunInstallersForHostEndpoints(hostDefinition, arguments);
            }

            var cfg = RunnerConfigurator.New(x =>
            {
                foreach (var endpoint in hostDefinition.Endpoints)
                {
                    ConfigureEndpointService(x, endpoint, arguments.CommandLineArgs);                    
                }

                if (arguments.Username != null && arguments.Password != null)
                {
                    x.RunAs(arguments.Username.Value, arguments.Password.Value);
                }
                else
                {
                    x.RunAsLocalSystem();
                }

                if (arguments.StartManually != null)
                {
                    x.DoNotStartAutomatically();
                }

                x.SetDisplayName(arguments.DisplayName != null ? arguments.DisplayName.Value : hostDefinition.DisplayName);
                x.SetServiceName(hostDefinition.ServiceName);
                x.SetDescription(arguments.Description != null ? arguments.Description.Value : "NServiceBus Message Endpoint Host Service for " + hostDefinition.DisplayName);

                var serviceCommandLine = arguments.Args.CustomArguments.AsCommandLine();
                serviceCommandLine += " /serviceName:\"" + hostDefinition.ServiceName + "\"";
                //serviceCommandLine += " /endpointName:\"" + endpointName + "\"";

                x.SetServiceCommandLine(serviceCommandLine);

                if (arguments.DependsOn == null)
                    x.DependencyOnMsmq();
                else
                    foreach (var dependency in arguments.DependsOn.Value.Split(','))
                        x.DependsOn(dependency);
            });

            Runner.Host(cfg, arguments.CommandLineArgs);
        }

        static void RunInstallersForHostEndpoints(HostDefinition host, HostArguments arguments)
        {
            foreach (var endpointDefinition in host.Endpoints)
            {
                WindowsInstaller.Install(arguments.CommandLineArgs,
                    endpointDefinition.ConfigurationType,
                    endpointDefinition.Name,
                    endpointDefinition.ConfigurationFile,
                    arguments.Install,
                    arguments.InstallInfrastructure != null);
            }
        }

        static void ConfigureEndpointService(IRunnerConfigurator config, EndpointInfo endpoint, string[] args)
        {
            //add the endpoint name so that the new appdomain can get it
            var endpointArgs = args.Concat(new[] { endpoint.Name }).ToArray();

            config.ConfigureServiceInIsolation<WindowsHost>(endpoint.ConfigurationType.AssemblyQualifiedName, c =>
            {
                c.ConfigurationFile(endpoint.ConfigurationFile);
                c.CommandLineArguments(endpointArgs, () => SetHostServiceLocatorArgs);
                c.WhenStarted(service => service.Start());
                c.WhenStopped(service => service.Stop());
                c.CreateServiceLocator(() => new HostServiceLocator { Args = args, EndpointName = endpoint.Name });
            });
        }

        static void InstallInfrastructure()
        {
            Configure.With(AllAssemblies.Except("NServiceBus.Host32.exe"));

            var installer = new Installer<Installation.Environments.Windows>(WindowsIdentity.GetCurrent());
            installer.InstallInfrastructureInstallers();
        }

        static HostDefinition DefineSingleEndpointHost(HostArguments arguments)
        {
            var endpointConfigurationType = GetEndpointConfigurationType(arguments);

            if (endpointConfigurationType == null)
                return null;

            AssertThatEndpointConfigurationTypeHasDefaultConstructor(endpointConfigurationType);

            var endpointName = EndpointDefinitionConventions.GetEndpointName(endpointConfigurationType, arguments);

            var endpointConfigurationFile = GetEndpointConfigurationFile(endpointConfigurationType);
            var endpointVersion = GetVersionFromEndpointConfigurationType(endpointConfigurationType);

            var serviceName = endpointName;
            var displayName = serviceName + "-" + endpointVersion;

            var endpoint = new EndpointInfo
            {
                Name = endpointName,
                ConfigurationType = endpointConfigurationType,
                ConfigurationFile = endpointConfigurationFile
            };

            var host = new HostDefinition
            {
                ServiceName = endpointName,
                DisplayName = displayName,
                Version = endpointVersion,
                Endpoints = new[] { endpoint }
            };

            return host;
        }

        static string GetVersionFromEndpointConfigurationType(Type endpointConfigurationType)
        {
            var fileVersion = FileVersionInfo.GetVersionInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, endpointConfigurationType.Assembly.ManifestModule.Name));

            //build a semver compliant version
            return string.Format("{0}.{1}.{2}", fileVersion.FileMajorPart, fileVersion.FileMinorPart, fileVersion.FileBuildPart);
        }

        static void DisplayHelpContent()
        {
            try
            {
                var stream = Assembly.GetCallingAssembly().GetManifestResourceStream("NServiceBus.Hosting.Windows.Content.Help.txt");

                if (stream != null)
                {
                    var helpText = new StreamReader(stream).ReadToEnd();

                    Console.WriteLine(helpText);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        static void SetHostServiceLocatorArgs(string[] args)
        {
        }

        static void AssertThatEndpointConfigurationTypeHasDefaultConstructor(Type type)
        {
            var constructor = type.GetConstructor(Type.EmptyTypes);

            if (constructor == null)
                throw new InvalidOperationException("Endpoint configuration type needs to have a default constructor: " + type.FullName);
        }

        static string GetEndpointConfigurationFile(Type endpointConfigurationType)
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, endpointConfigurationType.Assembly.ManifestModule.Name + ".config");
        }

        

        static Type GetEndpointConfigurationType(HostArguments arguments)
        {
            if (arguments.EndpointConfigurationType != null)
            {
                string t = arguments.EndpointConfigurationType.Value;
                if (t != null)
                {
                    Type endpointType = Type.GetType(t, false);
                    if (endpointType == null)
                        throw new ConfigurationErrorsException(string.Format("Command line argument 'endpointConfigurationType' has specified to use the type '{0}' but that type could not be loaded.", t));

                    return endpointType;
                }
            }

            string endpoint = ConfigurationManager.AppSettings["EndpointConfigurationType"];
            if (endpoint != null)
            {
                var endpointType = Type.GetType(endpoint, false);
                if (endpointType == null)
                    throw new ConfigurationErrorsException(string.Format("The 'EndpointConfigurationType' entry in the NServiceBus.Host.exe.config has specified to use the type '{0}' but that type could not be loaded.", endpoint));

                return endpointType;
            }

            IEnumerable<Type> endpoints = ScanAssembliesForEndpoints();
            AssertThatNotMoreThanOneEndpointIsDefined(endpoints);

            if ((endpoints.Count() == 0))
                return null;

            return endpoints.First();
        }

        static IEnumerable<Type> ScanAssembliesForEndpoints()
        {
            foreach (var assembly in AssemblyScanner.GetScannableAssemblies())
                foreach (Type type in assembly.GetTypes().Where(
                        t => typeof(IConfigureThisEndpoint).IsAssignableFrom(t)
                        && t != typeof(IConfigureThisEndpoint)
                        && !t.IsAbstract))
                {
                    yield return type;
                }
        }


        static void AssertThatNotMoreThanOneEndpointIsDefined(IEnumerable<Type> endpointConfigurationTypes)
        {
            if (endpointConfigurationTypes.Count() > 1)
            {
                throw new InvalidOperationException("Host doesn't support hosting of multiple endpoints. " +
                                                    "Endpoint classes found: " +
                                                    string.Join(", ",
                                                                endpointConfigurationTypes.Select(
                                                                    e => e.AssemblyQualifiedName).ToArray()) +
                                                    " You may have some old assemblies in your runtime directory." +
                                                    " Try right-clicking your VS project, and selecting 'Clean'."
                    );

            }
        }

    }

    public class HostDefinition
    {
        public string ServiceName { get; set; }
        public string Version { get; set; }
        public string DisplayName { get; set; }
        public EndpointInfo[] Endpoints { get; set; }
    }

    public class EndpointInfo
    {
        public string Name { get; set; }
        public Type ConfigurationType { get; set; }
        public string ConfigurationFile { get; set; }
    }
}
