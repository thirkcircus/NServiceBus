using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NServiceBus.Hosting.Helpers;
using NServiceBus.Hosting.Windows.Arguments;
using Topshelf;
using Topshelf.Configuration;

namespace NServiceBus.Hosting.Windows
{
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

            HostConventions.GetHostArguments = () => arguments;

            if (arguments.Help != null)
            {
                DisplayHelpContent();

                return;
            }

            RunHostInitializers();

            var endpoints = HostConventions.GetEndpointDefinitions().ToArray();

            if (endpoints.Length == 0)
            {
                if (arguments.InstallInfrastructure == null)
                    throw new InvalidOperationException("No endpoint configuration found in scanned assemblies. " +
                        "This usually happens when NServiceBus fails to load your assembly containing IConfigureThisEndpoint." +
                        " Try specifying the type explicitly in the NServiceBus.Host.exe.config using the appsetting key: EndpointConfigurationType, " +
                        "Scanned path: " + AppDomain.CurrentDomain.BaseDirectory);

                Console.WriteLine("Running infrastructure installers and exiting (ignoring other command line parameters if exist).");
                InstallInfrastructure();
                return;
            }

            var serviceName = HostConventions.GetServiceName();
            var displayName = HostConventions.GetServiceDisplayName();

            AppDomain.CurrentDomain.SetupInformation.AppDomainInitializerArguments = args;

            //TODO:Should we install for all endpoints?
            if ((arguments.Install) || (arguments.InstallInfrastructure != null))
                WindowsInstaller.Install(args, endpoints.First().ConfigurationFile);

            IRunConfiguration cfg = RunnerConfigurator.New(x =>
            {
                foreach (var e in endpoints)
                {
                    var endpoint = e;
                    x.ConfigureServiceInIsolation<WindowsHost>(endpoint.ConfigurationType.AssemblyQualifiedName, c =>
                    {
                        var endpointArgs = HostConventions.GetEndpointArguments(endpoint);
                        c.ConfigurationFile(endpoint.ConfigurationFile);
                        c.CommandLineArguments(endpointArgs.AsCommandLineArguments(), () => a => { });
                        c.WhenStarted(service => service.Start());
                        c.WhenStopped(service => service.Stop());
                        c.CreateServiceLocator(() => new HostServiceLocator(endpointArgs));
                    });
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

                x.SetDisplayName(arguments.DisplayName != null ? arguments.DisplayName.Value : displayName);
                x.SetServiceName(serviceName);
                x.SetDescription(arguments.Description != null ? arguments.Description.Value : "NServiceBus Message Endpoint Host Service for " + displayName);

                var serviceCommandLine = arguments.AsCommandLine();
                serviceCommandLine += " /serviceName:\"" + serviceName + "\"";
                
                //TODO:Is this needed?
                //serviceCommandLine += " /endpointName:\"" + endpointName + "\"";

                x.SetServiceCommandLine(serviceCommandLine);

                if (arguments.DependsOn == null)
                    x.DependencyOnMsmq();
                else
                    foreach (var dependency in arguments.DependsOn.Value.Split(','))
                        x.DependsOn(dependency);
            });

            Runner.Host(cfg, args);
        }

        static void RunHostInitializers()
        {
            var hostInitializers = HostConventions.GetScannableAssemblies()
                .AllInstancesAssignableTo<IWantCustomHostInitialization>();

            foreach (var initializer in hostInitializers)
            {
                initializer.Initialize();
            }
        }

        static void InstallInfrastructure()
        {
            Configure.With(AllAssemblies.Except("NServiceBus.Host32.exe"));

            var installer = new Installer<Installation.Environments.Windows>(WindowsIdentity.GetCurrent());
            installer.InstallInfrastructureInstallers();
        }
        
        static void DisplayHelpContent()
        {
            try
            {
                var stream = Assembly.GetCallingAssembly().GetManifestResourceStream("NServiceBus.Hosting.Windows.Content.Help.txt");

                if (stream == null) return;
                
                var helpText = new StreamReader(stream).ReadToEnd();

                Console.WriteLine(helpText);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}