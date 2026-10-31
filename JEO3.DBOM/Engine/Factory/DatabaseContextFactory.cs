using System.Diagnostics;
using JEO3.Engine.Models;
using JEO3.Providers;
using JEO3.Schema;

namespace JEO3.Engine
{
    /// <summary>
    /// Hydrates provider-specific flat metadata DTOs into the provider-agnostic JEO3.Schema object model.
    /// The Flat* layer is intentionally optimized for retrieval. This class is the normalization boundary
    /// between the flat database metadata representation and the metadata domain model.
    /// </summary>
    public static partial class DatabaseContextFactory
    {
        #region Entry Point

        public static async Task<DatabaseContext> GetContext(string connectionString, string providerName)
        {
            var provider = DatabaseProviderFactory.ToDatabaseProvider(providerName, connectionString);
            var flatCtx = await StagingContextFactory.GetFlatContext(connectionString, providerName, true);
            var ctx = GetContext(flatCtx, provider);

            return ctx;
        }

        private static DatabaseContext GetContext(IFlatDatabaseContext ctx, IDatabaseProvider provider)
        {
            try
            {
                DateTime now = DateTime.Now;
                var columns = ctx.Columns.OrderBy(v => v.SchemaName).ThenBy(v => v.Table).ThenBy(v => v.ColumnId).ToList();
                var relations = ctx.Relations.OrderBy(v => v.PrimaryColumnPath).ThenBy(v => v.ForeignColumnPath).ToList();
                var triggerModels = ctx.Triggers.OrderBy(v => v.Name).ToList();
                var indexes = ctx.Indexes.OrderBy(v => v.SchemaName).ThenBy(v => v.TableName).ThenBy(v => v.ColumnId).ToList();
                var procedures = ctx.Procedures.OrderBy(v => v.SchemaName).ThenBy(v => v.Name).ToList();

                var flatViews = ctx.Views?.ToList() ?? [];
                var flatFunctions = ctx.Functions?.ToList() ?? [];
                var flatUserDefinedTypes = ctx.UserDefinedTypes?.ToList() ?? [];
                var checkConstraintModels = ctx.CheckConstraints?.ToList() ?? [];
                var checkConstraintsByTable = checkConstraintModels.ToLookup(x => x.ParentObjectId);
                var missingIndexModels = ctx.MissingIndexes?.ToList() ?? [];

                // COLUMNS
                var columnsByObjectId = columns.GroupBy(x => x.ObjectId ?? 0).ToDictionary(x => x.Key, x => (IReadOnlyList<Schema.IColumn>)x.ToList());

                // INDEXES
                var indexHydration = HydrateIndexes(indexes);
                var indexModels = indexHydration.Select(x => x.Index).ToList();
                var indexesByTable = indexHydration.GroupBy(x => x.TableKey).ToDictionary(x => x.Key, x => (IReadOnlyList<IIndex>)x.Select(y => y.Index).ToList(), StringComparer.OrdinalIgnoreCase);

                // TABLES
                var tableList = ctx.Tables.OrderBy(v => v.DatabaseName).ThenBy(v => v.SchemaName).ThenBy(v => v.Name).ToList();

                // Database Set
                foreach (var db in ctx.Databases)
                {
                    db.Schemas = ctx.Schemas;
                }

                // Schemas Set
                foreach (var schema in ctx.Schemas)
                {
                    schema.Database = ctx.Databases.FirstOrDefault();
                    schema.Tables = tableList.Where(v => v.SchemaName == schema.Name).ToList();
                }

                // Tables Set
                foreach (var table in tableList)
                {
                    MapTable(table, columnsByObjectId, indexesByTable);
                    table.Database = ctx.Databases.FirstOrDefault();
                    table.Schema = ctx.Schemas.FirstOrDefault(v => v.Name == table.SchemaName);
                }
                var tablesByKey = tableList.ToDictionary(x => TableKey(x.SchemaName, x.Name), StringComparer.OrdinalIgnoreCase);
                var schemasByName = ctx.Schemas.ToDictionary(s => s.Name, StringComparer.OrdinalIgnoreCase);

                // RELATIONS / FOREIGN KEYS
                var relationModels = HydrateRelations(relations, tablesByKey);

                // RELATIONS BY TABLE
                var relationsByTable = relationModels
                    .SelectMany(relation => GetRelationTableKeys(relation).Select(tableKey => new { TableKey = tableKey, Relation = relation }))
                    .GroupBy(x => x.TableKey, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(x => x.Key, x => (IReadOnlyList<Schema.IRelation>)x.Select(y => y.Relation).ToList(), StringComparer.OrdinalIgnoreCase);

                // MISSING INDEXES BY TABLE
                var missingIndexesByTable = missingIndexModels.Where(x => x.ObjectId.HasValue).GroupBy(x => x.ObjectId!.Value).ToDictionary(x => x.Key, x => (IReadOnlyList<MissingIndex>)x.ToList());

                // CHECK CONSTRAINTS BY COLUMN PATH
                var constraintsByPath = checkConstraintModels.Where(cc => !string.IsNullOrEmpty(cc.Path)).GroupBy(cc => cc.Path!).ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

                // STATISTICS, TRIGGERS & EXTENDED PROPERTIES HYDRATION
                var statisticModels = ctx.Statistics?.ToList() ?? [];
                var statisticsByTable = statisticModels.ToLookup(x => x.ParentObjectId);
                var triggersByTable = triggerModels.GroupBy(x => x.ParentObjectId).ToDictionary(x => x.Key, x => (IReadOnlyList<ITrigger>)x.ToList());

                var extendedPropertyModels = ctx.ExtendedProperties?.ToList() ?? [];
                var extendedPropertiesByTable = extendedPropertyModels.Where(v => v.ParentObjectId != null).GroupBy(x => x.ParentObjectId!.Value).ToDictionary(x => x.Key, x => (IReadOnlyList<IExtendedProperty>)x.ToList());

                // TABLE NAVIGATION WIRING
                foreach (var table in tableList)
                {
                    var objectId = table.ObjectId.GetValueOrDefault();
                    var tableKey = TableKey(table.SchemaName, table.Name);

                    // TABLE -> MISSING INDEXES
                    var tableMissingIndexes = missingIndexesByTable.TryGetValue(objectId, out var missing) ? missing : Array.Empty<MissingIndex>();
                    table.MissingIndexes = tableMissingIndexes;

                    // TABLE -> STATISTICS
                    table.Statistics = statisticsByTable[objectId].ToList();

                    // TABLE -> TRIGGERS
                    table.Triggers = triggersByTable.TryGetValue(objectId, out var trig) ? trig : Array.Empty<ITrigger>();

                    // TABLE -> EXTENDED PROPERTIES
                    table.ExtendedProperties = extendedPropertiesByTable.TryGetValue(objectId, out var extProps) ? extProps : Array.Empty<IExtendedProperty>();

                    // TABLE -> RELATIONS / FOREIGN KEYS
                    var tableRelations = relationsByTable.TryGetValue(tableKey, out var relationsForTable) ? relationsForTable : Array.Empty<Schema.IRelation>();
                    table.Relations = tableRelations;

                    // TABLE -> CHECK CONSTRAINTS
                    table.CheckConstraints = checkConstraintsByTable[objectId].ToList();

                    // COLUMN -> CHECK CONSTRAINTS (exact column Path match: Schema.Table.Column)
                    foreach (var column in table.Columns)
                    {
                        if (column is Schema.Column concreteColumn && !string.IsNullOrEmpty(concreteColumn.Path))
                            concreteColumn.CheckConstraints = constraintsByPath.TryGetValue(concreteColumn.Path, out var colConstraints) ? colConstraints.Where(v => v != null).ToList() : [];

                        if (table.MissingIndexes.Count > 0)
                        {
                            ((Column)column).MissingIndexes = tableMissingIndexes.Where(v => v.EqualityColumns != null && v.EqualityColumnsString?.Contains(column.Name) == true).ToList();
                        }
                    }

                    // ForeignKeys on this table (this table is the Child holding the FK constraint)
                    table.ForeignKeys = tableRelations.OfType<IForeignKey>().Where(fk => fk.ReferencedTable != null && string.Equals(fk.ReferencedTable.SchemaName, table.SchemaName, StringComparison.OrdinalIgnoreCase) && string.Equals(fk.ReferencedTable.Name, table.Name, StringComparison.OrdinalIgnoreCase)).ToList();

                    // ReferencedByForeignKeys on this table (this table is the Parent / PK table being pointed to)
                    table.ReferencedByForeignKeys = tableRelations.OfType<IForeignKey>().Where(fk => fk.ParentTable != null && string.Equals(fk.ParentTable.SchemaName, table.SchemaName, StringComparison.OrdinalIgnoreCase) && string.Equals(fk.ParentTable.Name, table.Name, StringComparison.OrdinalIgnoreCase)).ToList();

                    // TABLE -> COLUMNS
                    if (columnsByObjectId.TryGetValue(objectId, out var tableColumns))
                    {
                        table.Columns = tableColumns;
                        foreach (var column in tableColumns)
                            if (column is Schema.Column metadataColumn) { metadataColumn.Table = table; }
                    }
                    // TABLE -> INDEXES
                    if (indexesByTable.TryGetValue(tableKey, out var tableIndexes))
                    {
                        table.Indexes = tableIndexes;
                        foreach (var index in tableIndexes)
                            if (index is Schema.Index metadataIndex) { metadataIndex.Table = table; }
                    }

                    // WIRE MISSING INDEX COLUMNS
                    foreach (var index in table.MissingIndexes)
                    {
                        var eqColNames = index.EqualityColumnsString?.Split('|').ToList() ?? [];
                        var ineqColNames = index.InequalityColumnsString?.Split('|').ToList() ?? [];
                        var inclColNames = index.IncludedColumnsString?.Split('|').ToList() ?? [];
                        var suggColNames = index.SuggestedKeyColumnsString?.Split('|').ToList() ?? [];
                        ((MissingIndex)index).IncludedColumns = table.Columns.Where(v => inclColNames.Contains(v.Name)).ToList();
                        ((MissingIndex)index).InequalityColumns = table.Columns.Where(v => ineqColNames.Contains(v.Name)).ToList();
                        ((MissingIndex)index).SuggestedKeyColumns = table.Columns.Where(v => suggColNames.Contains(v.Name)).ToList();
                        ((MissingIndex)index).EqualityColumns = table.Columns.Where(v => eqColNames.Contains(v.Name)).ToList();
                    }
                }

                // COLUMN INDEX LINKS
                var columnIndexLinks = indexes.Select(MapColumnIndexLink).ToList();

                // COLUMN INDEX LINK LOOKUPS
                var columnLinksByColumn = columnIndexLinks.GroupBy(x => new { x.ObjectName, x.ColumnId }).ToDictionary(x => x.Key, x => (IReadOnlyList<IColumnIndexLink>)x.ToList());

                var indexByTableAndId = tableList
                    .SelectMany(table => table.Indexes
                        .Where(i => i.IndexId.HasValue)
                        .Select(i => new
                        {
                            Key = (SchemaName: table.SchemaName, TableName: table.Name, IndexId: i.IndexId!.Value),
                            Index = i
                        }))
                    .ToDictionary(x => x.Key, x => x.Index);

                // COLUMN NAVIGATION WIRING
                foreach (var column in columns)
                {
                    // COLUMN -> INDEX LINKS
                    var columnTableName = column.TableName ?? string.Empty;
                    if (column.ColumnId.HasValue && columnLinksByColumn.TryGetValue(new { ObjectName = columnTableName, ColumnId = column.ColumnId }, out var columnLinks))
                        column.IndexLinks = columnLinks;

                    // COLUMN -> INDEXES (Using matching value tuple key instead of string interpolation)
                    column.Indexes = column.IndexLinks
                        .Select(link =>
                        {
                            var indexKey = (
                                SchemaName: column.SchemaName ?? string.Empty,
                                TableName: column.TableName ?? string.Empty,
                                IndexId: link.IndexId ?? 0L
                            );

                            return indexByTableAndId.TryGetValue(indexKey, out var index) ? index : null;
                        })
                        .Where(index => index != null)
                        .Cast<IIndex>()
                        .Distinct()
                        .ToList();
                }
                // COLUMN <-> FOREIGN KEY NAVIGATION
                var parentColumnForeignKeys = new Dictionary<Schema.Column, List<IForeignKey>>();
                var referencedColumnForeignKeys = new Dictionary<Schema.Column, List<IForeignKey>>();

                foreach (var foreignKey in relationModels)
                {
                    if (foreignKey.ParentTable is null || foreignKey.ReferencedTable is null) continue;

                    foreach (var pair in foreignKey.ColumnPairs)
                    {
                        // PARENT / FOREIGN KEY COLUMN
                        var parentColumn = foreignKey.ParentTable.Columns.FirstOrDefault(column => string.Equals(column.Name, pair.ParentColumnName, StringComparison.OrdinalIgnoreCase));
                        if (parentColumn is Schema.Column parentMetadataColumn)
                        {
                            if (!parentColumnForeignKeys.TryGetValue(parentMetadataColumn, out var fks)) parentColumnForeignKeys[parentMetadataColumn] = fks = [];
                            if (!fks.Contains(foreignKey)) fks.Add(foreignKey);
                        }
                        pair.ParentColumn = parentColumn as Schema.IColumn;

                        // REFERENCED COLUMN
                        var referencedColumn = foreignKey.ReferencedTable.Columns.FirstOrDefault(column => string.Equals(column.Name, pair.ReferencedColumnName, StringComparison.OrdinalIgnoreCase));
                        if (referencedColumn is Schema.Column referencedMetadataColumn)
                        {
                            if (!referencedColumnForeignKeys.TryGetValue(referencedMetadataColumn, out var fks)) referencedColumnForeignKeys[referencedMetadataColumn] = fks = [];
                            if (!fks.Contains(foreignKey)) fks.Add(foreignKey);
                        }
                        pair.ReferencedColumn = referencedColumn as Schema.IColumn;
                    }
                }

                foreach (var (col, fks) in parentColumnForeignKeys) col.ForeignKeys = fks;
                foreach (var (col, fks) in referencedColumnForeignKeys) col.ReferencedByForeignKeys = fks;

                // VIEW MODELS
                var viewModels = MapView(flatViews, columnsByObjectId).ToList();

                // VIEW COLUMN NAVIGATION (prevent view columns from thinking they belong to a Table)
                foreach (var view in viewModels)
                {
                    if (columnsByObjectId.TryGetValue(view.ObjectId.GetValueOrDefault(), out var viewColumns))
                        foreach (var column in viewColumns)
                            if (column is Schema.Column metadataColumn) metadataColumn.Table = null;
                }

                // PROCEDURES / FUNCTIONS / USER DEFINED TYPES
                var procedureModels = MapProcedure(procedures).ToList();
                var functionModels = MapFunction(flatFunctions).ToList();

                // SYNONYM & TARGET TABLE NAVIGATION PASS
                var synonymsByTargetTable = ctx.Synonyms.ToLookup(syn => TableKey(syn.TargetSchemaName, syn.TargetTableName), StringComparer.OrdinalIgnoreCase);

                foreach (var table in tableList)
                {
                    var tableKey = TableKey(table.SchemaName, table.Name);
                    if (synonymsByTargetTable.Contains(tableKey))
                    {
                        var matchedSynonyms = synonymsByTargetTable[tableKey].ToList();
                        foreach (var syn in matchedSynonyms) syn.TargetTable = table;
                        table.Synonyms = matchedSynonyms;
                    }
                }

                var finalColumns = tableList.SelectMany(v => v.Columns).Cast<Schema.IColumn>().ToList();
                var userDefinedTypeModels = MapUserDefinedType(flatUserDefinedTypes, finalColumns).ToList();

                var database = ctx.Databases.First();

                // FINAL CONTEXT
                // At this point every canonical object collection has been created and the graph
                // navigation has been wired. The same instances are exposed through every navigation path.
                var newCtx = new DatabaseContext
                {
                    Database = database,
                    Schemas = ctx.Schemas.ToList(),
                    Tables = tableList,
                    Views = viewModels,
                    Procedures = procedureModels,
                    Functions = functionModels,
                    UserDefinedTypes = userDefinedTypeModels,
                    Relations = relationModels.Cast<Schema.IRelation>().ToList(),
                    ForeignKeys = relationModels.Cast<IForeignKey>().ToList(),
                    Indexes = indexModels,
                    ColumnIndexLinks = columnIndexLinks,
                    Columns = finalColumns,
                    CheckConstraints = checkConstraintModels,
                    MissingIndexes = missingIndexModels,
                    Statistics = statisticModels,
                    Triggers = triggerModels,
                    Synonyms = synonymsByTargetTable.SelectMany(g => g).ToList(),
                    ExtendedProperties = extendedPropertyModels
                };

                var log = Math.Round((DateTime.Now - now).TotalSeconds, 2);
                Trace.WriteLine($"GetContext - Elapsed {log}sec");

                return newCtx;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        #endregion
    }
}