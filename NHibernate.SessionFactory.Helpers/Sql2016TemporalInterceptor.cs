using NHibernate;
using NHibernate.SqlCommand;
using System;
using System.Text.RegularExpressions;

namespace NHibernate.SessionFactory.Helpers
{
	[Serializable]
	public class Sql2016TemporalInterceptor : EmptyInterceptor
	{
		internal const string TEMPORAL_ASOF_COMMENT = "temporal-asof:";

		public static string GetTemporalQueryAsOf(string tableName, DateTime asOf)
		{
			return string.Format("{0}{1}[{2} {3}]", TEMPORAL_ASOF_COMMENT, tableName, asOf.ToShortDateString(), asOf.ToShortTimeString());
		}

		public override SqlString OnPrepareStatement(SqlString sql)
		{
			if (sql.ToString().Contains(TEMPORAL_ASOF_COMMENT))
			{
				sql = ApplyTemporalAsOf(sql, sql.ToString());
			}

			return base.OnPrepareStatement(sql);
		}

		private static SqlString ApplyTemporalAsOf(SqlString sql, string sqlString)
		{
			var indexOfTableName = sqlString.IndexOf(TEMPORAL_ASOF_COMMENT) + TEMPORAL_ASOF_COMMENT.Length;
			if (indexOfTableName < 0) { throw new InvalidOperationException("Temporal comment should contain name of table, like this: '/* temporal_asof: tableName */'"); }

			var indexOfTableNameEnd = sqlString.IndexOf("[", indexOfTableName + 1);
			if (indexOfTableNameEnd < 0) { throw new InvalidOperationException("Temporal comment should contain name of table, like this: '/* temporal_asof: tableName */'"); }

			var indexOfDateEnd = sqlString.IndexOf("]", indexOfTableNameEnd + 1);
			if (indexOfDateEnd < 0) { throw new InvalidOperationException("Temporal comment should contain name of table, like this: '/* temporal_asof: tableName */'"); }

			var tableName = sqlString.Substring(indexOfTableName, indexOfTableNameEnd - indexOfTableName).Trim();
			var dateParse = sqlString.Substring(indexOfTableNameEnd, indexOfDateEnd - indexOfTableNameEnd).Trim().Replace("[", string.Empty).Replace("]", string.Empty);

			var regex = new Regex(string.Format(@"{0}\s(\w+)", tableName));
			var aliasMatches = regex.Matches(sqlString, indexOfTableNameEnd);

			if (aliasMatches.Count == 0) { throw new InvalidOperationException("Could not find aliases for table with name: " + tableName); }

			var q = 0;
			foreach (Match aliasMatch in aliasMatches)
			{
				var alias = aliasMatch.Groups[1].Value;
				var aliasIndex = aliasMatch.Groups[1].Index + q; // + alias.Length;

				string temporal = string.Format(" FOR SYSTEM_TIME AS OF '{0}' ", dateParse);

				sql = sql.Insert(aliasIndex, temporal);
				q += temporal.Length;
			}
			return sql;
		}

		private static SqlString InsertTemporal(SqlString sql, string temporal)
		{
			// The original code used just "sql.Length". I found that the end of the sql string actually contains new lines and a semi colon.
			// Might need to change in future versions of NHibernate.
			var regex = new Regex(@"[^\;\s]", RegexOptions.RightToLeft);
			var insertAt = regex.Match(sql.ToString()).Index + 1;
			return sql.Insert(insertAt, temporal);
		}
	}
}