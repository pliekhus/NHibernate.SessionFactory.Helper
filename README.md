# NHibernate.SessionFactory.Helper
Extensions for the session factory to allow basic functionality and support for SQL2016 Temporal tables.

# Standard Usage
Inherit from the BaseSessionFactory and override the CreateSessionFactory.  This example uses FluentNHibernate to complete the configuration.

public class SessionFactory : BaseSessionFactory
	{
		public override ISessionFactory CreateSessionFactory(string connectionStringKey)
		{
			return Fluently.Configure()
				.Mappings(m => m.FluentMappings.AddFromAssemblyOf<NameAddressMap>())
				.Database(MsSqlConfiguration.MsSql2012.Dialect<MsSqlAzure2008Dialect>().ConnectionString(ConfigurationManager.ConnectionStrings[connectionStringKey].ConnectionString))
				.BuildSessionFactory();
		}
	}
