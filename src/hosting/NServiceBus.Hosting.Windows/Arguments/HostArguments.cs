using System.Linq;
using Topshelf.Internal;
using Topshelf.Internal.ArgumentParsing;

namespace NServiceBus.Hosting.Windows.Arguments
{
    internal class HostArguments
    {
        public HostArguments(string[] args)
        {
            CommandLineArgs = args;
            Args = Parser.ParseArgs(args);
            Install = Args.Install;
            Help = GetArgument(Args, "help") ?? GetArgument(Args, "?");
            ServiceName = GetArgument(Args, "serviceName");
            DisplayName = GetArgument(Args, "displayName");
            Description = GetArgument(Args, "description");
            EndpointConfigurationType = GetArgument(Args, "endpointConfigurationType");
            DependsOn = GetArgument(Args, "dependsOn");
            StartManually = GetArgument(Args, "startManually");
            Username = GetArgument(Args, "username");
            Password = GetArgument(Args, "password");
            SideBySide = GetArgument(Args, "sideBySide");
            EndpointName = GetArgument(Args, "endpointName");
            InstallInfrastructure = GetArgument(Args, "installInfrastructure");
        }

        public string[] CommandLineArgs { get; set; }
        public Parser.Args Args { get; private set; }
        public bool Install { get; set; }
        public IArgument SideBySide{ get; set; }
        public IArgument Help { get; set; }
        public IArgument ServiceName { get; set; }
        public IArgument DisplayName { get; set; }
        public IArgument Description { get; set; }
        public IArgument EndpointConfigurationType { get; set; }
        public IArgument DependsOn { get; set; }
        public IArgument StartManually { get; set; }
        public IArgument Username { get; set; }
        public IArgument Password { get; set; }
        public IArgument EndpointName { get; set; }
        public IArgument InstallInfrastructure{ get; set; }
        
        private static IArgument GetArgument(Parser.Args arguments, string key)
        {
            IArgument argument = arguments.CustomArguments.Where(x => x.Key != null).SingleOrDefault(x => x.Key.ToUpper() == key.ToUpper());

            if (argument != null)
            {
                arguments.CustomArguments = arguments.CustomArguments.Except(new[] {argument});
            }

            return argument;
        }
    }
}