using JEO3.Engine.Models;
using JEO3.Providers;
using JEO3.Schema;

namespace JEO3.Engine
{
    public static partial class DatabaseContextFactory
    {
        #region Tables

        private static void MapTable(Table source, IReadOnlyDictionary<int, IReadOnlyList<Schema.IColumn>> columnsByObjectId, IReadOnlyDictionary<string, IReadOnlyList<IIndex>> indexesByTable)
        {
            columnsByObjectId.TryGetValue(source.ObjectId.GetValueOrDefault(), out var columns);
            var tableKey = TableKey(source.SchemaName, source.Name);
            indexesByTable.TryGetValue(tableKey, out var indexes);

            source.Columns = columns ?? Array.Empty<Schema.IColumn>();
            source.Indexes = indexes ?? Array.Empty<IIndex>();
            source.Relations = Array.Empty<Schema.IRelation>();
            source.ForeignKeys = Array.Empty<IForeignKey>();
            source.ColumnIndexLinks = Array.Empty<IColumnIndexLink>();
        }

        #endregion
    }
}
