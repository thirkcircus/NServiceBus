using System;
using System.Linq;
using NServiceBus.Hosting.Windows;
using NServiceBus.Hosting.Windows.Arguments;
using NUnit.Framework;

namespace NServiceBus.Hosting.Tests.Windows.DefaultConventions
{
    public class GettingEndpointDefinitionsConcern : HostConventionsConcern
    {
        [TestFixtureSetUp]
        public override void Setup()
        {
            HostConventions.GetHostArguments = () => new HostArguments(new string[]{});
        }
    }

    public class When_getting_the_endpoint_definitions : GettingEndpointDefinitionsConcern
    {
        [Test]
        public void Should_define_the_endpoint()
        {
            HostConventions.GetEndpointTypes = () => new[] { typeof(EndpointA) };
            var endpoints = HostConventions.GetEndpointDefinitions().ToArray();

            Assert.AreEqual(endpoints.Length, 1);
            Assert.AreEqual(endpoints.First().ConfigurationType, typeof(EndpointA));
        }
    }

    public class When_getting_the_endpoint_definitions_and_there_is_more_than_one_endpoint_found : GettingEndpointDefinitionsConcern
    {
        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Should_raise_an_error()
        {
            HostConventions.GetEndpointTypes = () => new[] { typeof(EndpointA), typeof(EndpointB) };
            var endpoint = HostConventions.GetEndpointDefinitions();
        }
    }
}