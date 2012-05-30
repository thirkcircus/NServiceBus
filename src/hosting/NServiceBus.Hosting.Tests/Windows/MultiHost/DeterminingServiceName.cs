using System;
using NServiceBus.Hosting.Windows;
using NServiceBus.Hosting.Windows.Arguments;
using NServiceBus.Hosting.Windows.MultiHost;
using NUnit.Framework;

namespace NServiceBus.Hosting.Tests.Windows.MultiHost
{
    public class When_determining_the_service_name_using_the_multi_host_configuration_and_a_service_name_argument_is_specified : HostConventionsConcern
    {
        [Test]
        public void Should_use_the_service_name_provided_in_the_argument()
        {
            HostConventions.GetHostArguments = () =>
                new HostArguments(new[] { "/multihost","/ServiceName:CustomServiceName" });

            new MultiHostConventions()
                .Initialize();
            
            HostConventions.GetServiceName();
        }
    }

    public class When_determining_the_service_name_using_the_multi_host_configuration_and_no_service_name_argument_is_specified : HostConventionsConcern
    {
        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Should_fail()
        {
            HostConventions.GetHostArguments = () =>
                new HostArguments(new[] { "/multihost" });

            new MultiHostConventions()
                .Initialize();

            HostConventions.GetServiceName();
        }
    }
}