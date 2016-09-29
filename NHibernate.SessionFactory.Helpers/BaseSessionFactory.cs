using NHibernate.Persister.Entity;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace NHibernate.SessionFactory.Helpers
{
	/// <summary>
	/// BaseSessionFactory to provide shared functionality when accessing NHibernate persistence
	/// </summary>
	public abstract class BaseSessionFactory
	{
		/// <summary>
		/// Static collection of the session factories under management
		/// </summary>
		public static IDictionary<string, ISessionFactory> factories = new Dictionary<string, ISessionFactory>();

		/// <summary>
		/// Gets all of instances of type T from the NHibernate maps.
		/// </summary>
		/// <typeparam name="S">The session factory type</typeparam>
		/// <typeparam name="T">The class type to hydrate</typeparam>
		/// <param name="connectionStringKey">The connection string key.</param>
		/// <returns>An enumerated list of the type T</returns>
		public static IEnumerable<T> GetAllOf<S, T>(string connectionStringKey)
			where S : BaseSessionFactory, new()
			where T : class
		{
			return GetAllOfCriteria<S, T>(connectionStringKey).List<T>();
		}

		/// <summary>
		/// Gets all of the instance of type T from a SQL string.
		/// </summary>
		/// <typeparam name="S">The session factory type</typeparam>
		/// <typeparam name="T">The class type to hydrate</typeparam>
		/// <param name="connectionStringKey">The connection string key.</param>
		/// <param name="sql">The sql string used to hydrate the objects</param>
		/// <returns>An enumerated list of the type T</returns>
		public static IEnumerable<T> GetAllOf<S, T>(string connectionStringKey, string sql)
			where S : BaseSessionFactory, new()
			where T : class
		{
			ISQLQuery query = GetSqlQuery<S>(connectionStringKey, sql);
			query.AddEntity(typeof(T));
			return query.List<T>();
		}

		/// <summary>
		/// Gets a criteria to support getting all of type T from the NHibernate maps.
		/// </summary>
		/// <typeparam name="S">The session factory type</typeparam>
		/// <typeparam name="T">The class type to hydrate</typeparam>
		/// <param name="connectionStringKey">The connection string key.</param>
		/// <returns>a criteria for the enumerated list of the type T</returns>
		public static ICriteria GetAllOfCriteria<S, T>(string connectionStringKey)
			where S : BaseSessionFactory, new()
			where T : class
		{
			return GetAllOfCriteria<S, T>(GetSession<S>(connectionStringKey));
		}

		public static ICriteria GetAllOfCriteria<S, T>(ISession session)
					where S : BaseSessionFactory, new()
					where T : class
		{
			return GetAllOfCriteriaAsOf<S, T>(session, DateTime.Now);
		}

		public static ICriteria GetAllOfCriteriaAsOf<S, T>(string connectionStringKey, DateTime asOf)
					where S : BaseSessionFactory, new()
					where T : class
		{
			return GetAllOfCriteriaAsOf<S, T>(GetSession<S>(connectionStringKey), asOf);
		}

		public static ICriteria GetAllOfCriteriaAsOf<S, T>(ISession session, DateTime asOf)
					where S : BaseSessionFactory, new()
					where T : class
		{
			ICriteria criteria = session
				.CreateCriteria<T>();
			//.SetMaxResults(500) //for testing
			;

			int maxResults = 0;
			int.TryParse(ConfigurationManager.AppSettings["NHibernate.MaxResults"], out maxResults);
			if (maxResults > 0) { criteria.SetMaxResults(maxResults); }

			if (typeof(T).GetInterface(typeof(ITemporal).FullName) != null)
			{
				string tableName = ((AbstractEntityPersister)(session.SessionFactory)
						.GetClassMetadata(typeof(T))).TableName;

				criteria.SetComment(Sql2016TemporalInterceptor.GetTemporalQueryAsOf(tableName, asOf));
			}

			return criteria;
		}

		/// <summary>
		/// Gets a single instance of type T from the NHubernate maps
		/// </summary>
		/// <typeparam name="S">The session factory type</typeparam>
		/// <typeparam name="T">The class type to hydrate</typeparam>
		/// <param name="connectionStringKey">The connection string key.</param>
		/// <returns>A hydrated instance of type T</returns>
		public static ICriteria GetItemOf<S, T>(string connectionStringKey)
			where S : BaseSessionFactory, new()
			where T : class
		{
			return GetSession<S>(connectionStringKey)
				.CreateCriteria<T>();
		}

		/// <summary>
		/// Gets the session for the specified implementation type
		/// </summary>
		/// <typeparam name="S">The session factory type</typeparam>
		/// <param name="connectionStringKey">The connection string key.</param>
		/// <returns>The session for the specified connection string key</returns>
		public static ISession GetSession<S>(string connectionStringKey)
			where S : BaseSessionFactory, new()
		{
			ISessionFactory sessionFactory = (new S()).GetCurrent(connectionStringKey);
			return sessionFactory.OpenSession();
		}

		/// <summary>
		/// Gets a session based SQL query
		/// </summary>
		/// <typeparam name="S">The session factory type</typeparam>
		/// <typeparam name="T">The class type to hydrate</typeparam>
		/// <param name="query">The sql query or stored procedure name</param>
		/// <returns>The session based sql query</returns>
		public static ISQLQuery GetSqlQuery<S>(string connectionStringKey, string query)
			where S : BaseSessionFactory, new()
		{
			return GetSession<S>(connectionStringKey).CreateSQLQuery(query);
		}

		public static string GetTableName<S, T>(string connectionStringKey)
					where S : BaseSessionFactory, new()
					where T : class, new()
		{
			return GetSession<S>(connectionStringKey)
				.GetSessionImplementation()
				.GetEntityPersister(null, new T()).RootEntityName;
		}

		public static IQueryable<T> Query<S, T>(string connectionStringKey)
					where S : BaseSessionFactory, new()
					where T : class
		{
			return QueryAsOf<S, T>(connectionStringKey, DateTime.Now);
		}

		public static IQueryable<T> QueryAsOf<S, T>(string connectionStringKey, DateTime asOf)
					where S : BaseSessionFactory, new()
					where T : class
		{
			return GetAllOfCriteriaAsOf<S, T>(GetSession<S>(connectionStringKey), asOf)
				.List<T>()
				.AsQueryable();
		}

		public static IQueryOver<T> QueryOver<S, T>(string connectionStringKey)
					where S : BaseSessionFactory, new()
					where T : class
		{
			return GetSession<S>(connectionStringKey)
				.QueryOver<T>();
		}

		/// <summary>
		/// Saves the instance of type T based on the provided SQL (i.e. stored procedure name).
		/// </summary>
		/// <typeparam name="S">The session factory type</typeparam>
		/// <param name="connectionStringKey">The connection string key.</param>
		/// <param name="query">The sql query or stored procedure name</param>
		/// <param name="parameters">The sql parameters needed by the provided sql</param>
		/// <returns>Boolean value indicating the SQL was executed</returns>
		public static bool SaveItem<S>(string connectionStringKey, string query, params KeyValuePair<string, string>[] parameters)
			where S : BaseSessionFactory, new()
		{
			ISQLQuery sqlQuery = GetSqlQuery<S>(connectionStringKey, query);
			parameters.ToList().ForEach(p => sqlQuery.SetParameter(p.Key, p.Value));
			IList<int> retVal = sqlQuery.List<int>();

			return (retVal.Count > 0 && retVal.First() == 0 ? true : false);
		}

		[Obsolete("Used for legacy support only.  Once legacy system are turned off this will be as well.  We should be using stored procedures for all saves going forward to support Temporal structures.")]
		public static bool SaveItem<S, T>(string connectionStringKey, T item)
					where S : BaseSessionFactory, new()
		{
			ISession session = GetSession<S>(connectionStringKey);
			session.SaveOrUpdate(item);
			return true;
		}

		/// <summary>
		/// Creates the session factory.  Allowed to override per implementation
		/// </summary>
		/// <param name="connectionStringKey">The connection string key.</param>
		/// <returns>A session factory implementation</returns>
		public abstract ISessionFactory CreateSessionFactory(string connectionStringKey);

		/// <summary>
		/// Gets the current session factory, or creates a new one, based on the connection string key
		/// </summary>
		/// <param name="connectionStringKey">The connection string key.</param>
		/// <returns>An instance of the session factory</returns>
		public ISessionFactory GetCurrent(string connectionStringKey)
		{
			if (!factories.ContainsKey(connectionStringKey))
			{
				factories.Add(connectionStringKey, CreateSessionFactory(connectionStringKey));
			}

			return factories[connectionStringKey];
		}
	}
}