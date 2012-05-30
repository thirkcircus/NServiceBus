using NServiceBus.Hosting.Windows;

namespace NServiceBus.Hosting.Tests.Windows
{
    public class HostConventionsConcern
    {
        [NUnit.Framework.TestFixtureSetUp]
        public virtual void Setup()
        {
            HostConventions.RevertToDefaultConventions();
        }
    }
}
