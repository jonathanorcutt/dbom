using System.Data;
using System.Data.Common;
using JEO3.Providers.Catalogs;
using Microsoft.Data.Sqlite;

namespace JEO3.Providers
{
    internal sealed class SqliteDatabasesProvider : DatabaseProvider
    {
        #region Initialization
        internal SqliteDatabasesProvider(IConnectionDto connection)
            : base(connection)
        {
            this.SchemaCatalog = new MSSchemaQueryCatalog();
        }
        #endregion

        #region Functions
        public override async Task<DataSet> GetDataset(string query)
        {
            DataSet dataSet = new DataSet();
            using (SqliteConnection connection = new SqliteConnection(this.Connection?.ConnectionString))
            {
                using (SqliteCommand command = new SqliteCommand(query, connection))
                {
                    DbProviderFactory factory = DbProviderFactories.GetFactory(connection);
                    using (DbDataAdapter adapter = factory.CreateDataAdapter())
                    {
                        adapter.SelectCommand = command;
                        await Task.Run(() => adapter.Fill(dataSet));
                    }
                }
            }
            return dataSet;
        }
        public override Task<DataTable> GetDataTable(string query)
        {
            DataTable dataTable = new DataTable();

            using (var connection = new SqliteConnection(this.Connection.ConnectionString))
            {
                connection.Open();
                using (var cmd = new SqliteCommand(query, connection))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        // Automatically maps the reader columns and rows to the DataTable
                        dataTable.Load(reader);
                    }
                }
            }

            return Task.FromResult(dataTable);
        }
        #endregion
    }
}
