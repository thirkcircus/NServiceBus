using System;

namespace NServiceBus.Hosting.Windows
{
    /// <summary>
    /// A class that represents an endpoint to host
    /// </summary>
    public class EndpointDefinition
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="configurationType"></param>
        /// <param name="name"></param>
        /// <param name="configurationFile"></param>
        public EndpointDefinition(Type configurationType, string name, string configurationFile)
        {
            ConfigurationType = configurationType;
            Name = name;
            ConfigurationFile = configurationFile;
        }

        /// <summary>
        /// The name of the endpoint, which serves to identify the endpoint
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The type that will be used to configure the endpoint
        /// </summary>
        public Type ConfigurationType { get; set; }

        /// <summary>
        /// The application configuration file (app.config) that should be used when hosting the endpoint
        /// </summary>
        public string ConfigurationFile { get; set; }
    }
}