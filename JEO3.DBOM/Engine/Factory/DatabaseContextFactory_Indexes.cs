using JEO3.Engine.Models;
using JEO3.Providers;
using JEO3.Schema;

namespace JEO3.Engine
{
    public static partial class DatabaseContextFactory
    {
        #region Indexes

        private sealed record HydratedIndex(string TableKey, Schema.Index Index);

        private static IReadOnlyList<HydratedIndex> HydrateIndexes(IReadOnlyList<FlatIndex> source)
        {
            return source
                .GroupBy(x => new { x.SchemaName, x.TableName, x.IndexId })
                .Select(group =>
                {
                    var first = group.First();
                    var indexColumns = group.OrderBy(x => x.IndexOrdinalPosition).ThenBy(x => x.IndexColumnId).Select(MapIndexColumn).ToList();
                    var tableKey = TableKey(first.SchemaName, first.TableName);

                    var index = new Schema.Index
                    {
                        ObjectId = null,
                        SchemaName = first.SchemaName,
                        IndexId = first.IndexId,
                        Name = first.Name,
                        IndexType = MapIndexType(first.IndexType),
                        ProviderIndexType = first.IndexType,
                        IsUnique = first.IsUnique,
                        IsPrimaryKey = false,
                        IsUniqueConstraint = first.IsUniqueConstraint,
                        IsDisabled = first.IsDisabled,
                        IsClustered = first.IsClustered,
                        IsCovering = group.Any(x => x.IsIncluded),
                        FragmentationPercentage = first.FragmentationPercentage,
                        PageCount = first.PageCount,
                        Columns = indexColumns,
                        KeyColumns = group.Where(x => !x.IsIncluded).OrderBy(x => x.IndexOrdinalPosition).Select(MapIndexColumnAsColumn).Cast<Schema.IColumn>().ToList(),
                        IncludedColumns = group.Where(x => x.IsIncluded).OrderBy(x => x.IndexOrdinalPosition).Select(MapIndexColumnAsColumn).Cast<Schema.IColumn>().ToList()
                    };

                    return new HydratedIndex(tableKey, index);
                })
                .ToList();
        }

        private static IndexColumn MapIndexColumn(FlatIndex source)
        {
            return new IndexColumn
            {
                ObjectId = null,
                IndexId = source.IndexId,
                ColumnId = source.ColumnId,
                IndexName = source.Name,
                ColumnName = source.ColumnName,
                Ordinal = source.IndexOrdinalPosition,
                IsIncluded = source.IsIncluded,
                IsKeyColumn = !source.IsIncluded,
                SortDirection = IndexColumnSortDirection.Unknown,
                IsDescending = false
            };
        }

        private static Schema.Column MapIndexColumnAsColumn(FlatIndex source)
        {
            return new Schema.Column
            {
                ObjectId = null,
                ColumnId = source.ColumnId,
                Name = source.ColumnName,
                DataType = source.DataType,
                ProviderDataType = source.DataType,
                TableName = source.TableName,
                SchemaName = source.SchemaName,
                IsIndexedInDatabase = true
            };
        }

        private static ColumnIndexLink MapColumnIndexLink(FlatIndex source)
        {
            return new ColumnIndexLink
            {
                ObjectId = null,
                ColumnId = source.ColumnId,
                IndexId = source.IndexId,
                ObjectName = source.TableName,
                ColumnName = source.ColumnName,
                IndexName = source.Name,
                IsIncludedColumn = source.IsIncluded,
                OrdinalPosition = source.IsIncluded ? null : source.IndexOrdinalPosition,
                IncludedOrdinal = source.IsIncluded ? source.IndexOrdinalPosition : null,
                SortDirection = IndexColumnSortDirection.Unknown,
                IsDescending = false,
                IsIndexDisabled = source.IsDisabled,
                IsUnique = source.IsUnique,
                IsPrimaryKey = false,
                IsUniqueConstraint = source.IsUniqueConstraint
            };
        }

        private static IndexType MapIndexType(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? IndexType.Unknown : value.Trim().ToLowerInvariant() switch
            {
                "heap" => IndexType.Heap,
                "clustered" => IndexType.Clustered,
                "nonclustered" => IndexType.NonClustered,
                "non-clustered" => IndexType.NonClustered,
                "hash" => IndexType.Hash,
                "columnstore" => IndexType.ColumnStore,
                _ => IndexType.Other
            };
        }

        #endregion
    }
}
