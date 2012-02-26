using System;
using NServiceBus.Hosting.Windows.Arguments;

namespace NServiceBus.Hosting.Windows.Conventions
{
    internal static class EndpointDefinitionConventions
    {
        /// <summary>
        /// Gives a string which serves to identify the endpoint.
        /// </summary>
        /// <param name="endpointConfigurationType"></param>
        /// <param name="arguments"> </param>
        /// <returns></returns>
        internal static string GetEndpointName(Type endpointConfigurationType, HostArguments arguments)
        {
            if (arguments.ServiceName != null)
                return arguments.ServiceName.Value;

            var endpointName = GetEndpointNameFromAttribute(endpointConfigurationType);

            if (endpointName != null) 
                return endpointName;

            endpointName = GetEndpointNameFromINameThisEndpoint(endpointConfigurationType);

            if (endpointName != null) return endpointName;

            if (arguments.EndpointName != null)
                return arguments.EndpointName.Value;

            return endpointConfigurationType.Namespace;
        }

        public static string GetEndpointNameFromINameThisEndpoint(Type endpointConfigurationType)
        {
            var endpointConfiguration = Activator.CreateInstance(endpointConfigurationType);
            
            if (endpointConfiguration is INameThisEndpoint)
                return (endpointConfiguration as INameThisEndpoint).GetName();

            return null;
        }

        public static string GetEndpointNameFromAttribute(Type endpointConfigurationType)
        {
            var arr = endpointConfigurationType.GetCustomAttributes(typeof (EndpointNameAttribute), false);

            return arr.Length == 1 ? (arr[0] as EndpointNameAttribute).Name : null;
        }
    }
}