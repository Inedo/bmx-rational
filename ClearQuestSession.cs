using System;
using System.Collections.Generic;
using System.Data;

namespace Inedo.BuildMasterExtensions.Rational
{
    /// <summary>
    /// Represents a ClearQuest session.
    /// </summary>
    internal sealed class ClearQuestSession : IDisposable
    {
        private readonly ClearQuestOleServer.SessionClass session;
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClearQuestSession"/> class.
        /// </summary>
        /// <param name="session">The session.</param>
        private ClearQuestSession(ClearQuestOleServer.SessionClass session)
        {
            this.session = session;
        }

        /// <summary>
        /// Gets the available databases.
        /// </summary>
        /// <param name="masterDatabase">The master database.</param>
        /// <returns>Array of available database names.</returns>
        public static string[] GetAvailableDatabases(string masterDatabase)
        {
            var session = new ClearQuestOleServer.SessionClass();
            var coll = (object[])session.GetAccessibleDatabases(masterDatabase, "", "");
            var dbNames = new List<string>();

            foreach (ClearQuestOleServer.IOAdDatabaseDesc desc in coll)
                dbNames.Add(desc.GetDatabaseName());

            return dbNames.ToArray();
        }

        /// <summary>
        /// Connects to a ClearQuest database using specified credentials.
        /// </summary>
        /// <param name="userName">Name of the user.</param>
        /// <param name="password">The user's password.</param>
        /// <param name="database">The database to connect to.</param>
        /// <returns>Newly-opened session.</returns>
        public static ClearQuestSession Connect(string userName, string password, string database)
        {
            var session = new ClearQuestOleServer.SessionClass();
            session.UserLogon(userName, password, database, 1, string.Empty);
            return new ClearQuestSession(session);
        }

        /// <summary>
        /// Runs a query on an entity table.
        /// </summary>
        /// <param name="entityName">Name of the entity.</param>
        /// <param name="fields">The fields to return.</param>
        /// <returns>DataTable containing the query results.</returns>
        public DataTable RunQuery(string entityName, params string[] fields)
        {
            return RunFilteredQuery(entityName, null, fields);
        }

        /// <summary>
        /// Runs a filtered query on an entity table.
        /// </summary>
        /// <param name="entityName">Name of the entity.</param>
        /// <param name="filter">The field filter values.</param>
        /// <param name="fields">The fields to return.</param>
        /// <returns>DataTable containing the query results.</returns>
        public DataTable RunFilteredQuery(string entityName, IEnumerable<KeyValuePair<string, object>> filter, params string[] fields)
        {
            var table = new DataTable();
            foreach (var field in fields)
                table.Columns.Add(field, typeof(object));

            var query = (ClearQuestOleServer.IOAdQueryDef)session.BuildQuery(entityName);
            foreach (var field in fields)
                query.BuildField(field);

            if (filter != null)
            {
                foreach (var pair in filter)
                {
                    var filterOp = (ClearQuestOleServer.IOAdQueryFilterNode)query.BuildFilterOperator(1);
                    filterOp.BuildFilter(pair.Key, 1, pair.Value);
                }
            }

            var results = (ClearQuestOleServer.IOAdResultSet)session.BuildResultSet(query);
            results.Execute();

            int columns = results.GetNumberOfColumns();

            while (results.MoveNext() == 1)
            {
                var row = table.NewRow();

                for (int i = 1; i <= columns; i++)
                    row[results.GetColumnLabel(i)] = results.GetColumnValue(i);

                table.Rows.Add(row);
            }

            return table;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (!this.disposed)
            {
                session.SignOff();
                this.disposed = true;
            }
        }
    }
}
