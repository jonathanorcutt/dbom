using System.Data;
using JEO3.Providers.Catalogs;

namespace JEO3.Providers
{
    internal interface IDatabaseProvider
    {
        IConnectionDto Connection { get; }
        ISchemaQueryCatalog SchemaCatalog { get; }

        Task<DataSet> GetDataset(string query);
        Task<DataTable> GetDataTable(string query);
        Task<IReadOnlyList<T>> GetInstances<T>(string query) where T : class, new();
        Task<IReadOnlyList<T>> GetInstances<T>(SchemaQueryType type) where T : class, new();
    }
}