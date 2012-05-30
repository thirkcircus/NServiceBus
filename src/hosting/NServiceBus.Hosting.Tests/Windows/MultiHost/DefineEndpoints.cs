using System.Linq;
using NServiceBus.Hosting.Windows;
using NServiceBus.Hosting.Windows.Arguments;
using NServiceBus.Hosting.Windows.MultiHost;
using NUnit.Framework;

namespace NServiceBus.Hosting.Tests.Windows.MultiHost
{
    public class When_getting_the_endpoints_to_host_using_the_multi_host_configuration_and_an_endpoints_argument_is_specified : HostConventionsConcern
    {
        [Test]
        [Ignore("Not implemented")]
        public void Should_return_only_the_endpoints_that_were_specified()
        {
            MultiHostConventions.SetMultihostConventions();

            HostConventions.GetHostArguments = () =>
                new HostArguments(new[] { "/Endpoints:NServiceBus.Hosting.Tests.Windows.EndpointA" });

            HostConventions.GetEndpointTypes = () => new[] { typeof(EndpointA), typeof(EndpointB) };
            var endpoints = HostConventions.GetEndpointDefinitions().ToArray();

            Assert.AreEqual(endpoints.Length, 2);
        }
    }

    public class When_getting_the_endpoints_to_host_using_the_multi_host_configuration_and_an_endpoints_argument_is_specified_with_an_endpoint_that_does_not_exist : HostConventionsConcern
    {
        [Test]
        [Ignore("Not implemented")]
        public void Should_fail()
        {
            MultiHostConventions.SetMultihostConventions();
            
            HostConventions.GetHostArguments = () => 
                new HostArguments(new[] { "/Endpoints:NServiceBus.Hosting.Tests.Windows.EndpointA, NServiceBus.Hosting.Tests.Windows.EndpointC" });

            HostConventions.GetEndpointTypes = () => new[] { typeof(EndpointA), typeof(EndpointB) };
            var endpoints = HostConventions.GetEndpointDefinitions().ToArray();

            Assert.AreEqual(endpoints.Length, 2);
        }
    }

    public class When_getting_the_endpoints_to_using_the_multi_host_configuration_and_an_endpoint_argument_is_not_specified : HostConventionsConcern
    {
        [Test]
        public void Should_return_all_endpoint_configurations()
        {
            MultiHostConventions.SetMultihostConventions();
            HostConventions.GetEndpointTypes = () => new[] { typeof(EndpointA), typeof(EndpointB) };
            var endpoints = HostConventions.GetEndpointDefinitions().ToArray();

            Assert.AreEqual(endpoints.Length, 2);
        }
    }
}