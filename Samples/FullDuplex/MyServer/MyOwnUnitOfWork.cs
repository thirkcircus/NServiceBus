namespace MyServer
{
    using System;
    using NServiceBus;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.UnitOfWork;

    public class MyOwnUnitOfWork : IManageUnitsOfWork
    {
        public IMySession MySession { get; set; }

        public void Begin()
        {
            LogMessage("Begin");
        }

        public void End(Exception ex)
        {
            if (ex == null)
            {
                LogMessage("Commit");
                MySession.SaveChanges();
            }
            else
                LogMessage("Rollback, reason: " + ex);
        }

        void LogMessage(string message)
        {
            Console.WriteLine(string.Format("UoW({0}) - {1}",GetHashCode(), message));
        }
    }

    public interface IMySession
    {
        void SaveChanges();
    }

    public class ExampleSession:IMySession
    {
        public void SaveChanges()
        {
            Console.WriteLine(string.Format("ExampleSession({0}) - {1}", GetHashCode(), "Saving changes"));
        }
    }

    public class UoWIntitializer : IWantCustomInitialization
    {
        public void Init()
        {
            Configure.Instance.Configurer.
                ConfigureComponent<MyOwnUnitOfWork>(DependencyLifecycle.InstancePerUnitOfWork);

            //this shows the new lambda feature introduced in NServiceBus 3.2.3
            Configure.Instance.Configurer.
                ConfigureComponent<IMySession>(()=> new ExampleSession(),DependencyLifecycle.InstancePerUnitOfWork);

        }
    }
}