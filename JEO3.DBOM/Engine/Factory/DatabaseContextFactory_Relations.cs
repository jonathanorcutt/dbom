using JEO3.Engine.Models;
using JEO3.Providers;
using JEO3.Schema;

namespace JEO3.Engine
{
    public static partial class DatabaseContextFactory
    {
        #region Relations

        private static IReadOnlyList<ForeignKey> HydrateRelations(IReadOnlyList<FlatRelation> source, IReadOnlyDictionary<string, Table> tablesByKey)
        {
            return source
                .GroupBy(x => new { x.ObjectId, x.KeyName })
                .Select(group =>
                {
                    var first = group.First();
                    var primaryKey = TableKey(first.PrimarySchema, first.PrimaryTableName);
                    var foreignKey = TableKey(first.ForeignSchema, first.ForeignTableName);
                    tablesByKey.TryGetValue(primaryKey, out var primaryTable);
                    tablesByKey.TryGetValue(foreignKey, out var foreignTable);
                    var pairs = group.OrderBy(x => x.PrimaryColumnPath).ThenBy(x => x.ForeignColumnPath).Select((x, ordinal) => MapRelationColumnPair(x, ordinal + 1)).ToList();

                    return new ForeignKey
                    {
                        ObjectId = group.Key.ObjectId,
                        RelationId = first.ObjectId,
                        Name = first.KeyName,
                        RelationType = MapRelationType(first.KeyType),
                        KeyName = first.KeyName,
                        // ParentTable = PK side (e.g. Product), ReferencedTable = FK-holding side (e.g. ProductInventory).
                        // This naming is intentional in this codebase - opposite of the "Parent = FK holder" convention
                        // some other schema tools use. Keep consistent with RelationColumnPair below.
                        ParentTable = primaryTable,
                        ParentObjectId = primaryTable?.ObjectId,
                        ReferencedTable = foreignTable,
                        ReferencedObjectId = foreignTable?.ObjectId,
                        ColumnPairs = pairs,
                        IsDisabled = first.IsDisabled,
                        DeleteAction = first.DeleteAction,
                        UpdateAction = first.UpdateAction,
                        IsNullable = group.Any(x => x.IsNullable),
                        IsSelfReferencing = string.Equals(first.PrimarySchema, first.ForeignSchema, StringComparison.OrdinalIgnoreCase) && string.Equals(first.PrimaryTableName, first.ForeignTableName, StringComparison.OrdinalIgnoreCase),
                        IsComposite = first.IsComposite || group.Count() > 1
                    };
                })
                .ToList();
        }

        private static RelationColumnPair MapRelationColumnPair(FlatRelation source, int ordinal)
        {
            return new RelationColumnPair
            {
                RelationId = source.ObjectId,
                Ordinal = ordinal,
                // Parent = PK column (Product.ProductID)
                ParentColumnName = source.PrimaryColumnName,
                ParentTableName = source.PrimaryTableName,
                // Referenced = FK column (ProductInventory.ProductID)
                ReferencedColumnName = source.ForeignColumnName,
                ReferencedTableName = source.ForeignTableName
            };
        }

        private static RelationType MapRelationType(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? RelationType.ForeignKey : value.Trim().ToLowerInvariant() switch
            {
                "foreign key" => RelationType.ForeignKey,
                "foreignkey" => RelationType.ForeignKey,
                "reference" => RelationType.Reference,
                "dependency" => RelationType.Dependency,
                _ => RelationType.ForeignKey
            };
        }

        private static IEnumerable<string> GetRelationTableKeys(Schema.IRelation relation)
        {
            if (relation.ParentTable != null) yield return TableKey(relation.ParentTable.SchemaName, relation.ParentTable.Name);
            if (relation.ReferencedTable != null) yield return TableKey(relation.ReferencedTable.SchemaName, relation.ReferencedTable.Name);
        }

        #endregion
    }
}
