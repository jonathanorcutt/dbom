using JEO3.Engine.Models;
using JEO3.Providers;
using JEO3.Schema;

namespace JEO3.Engine
{
    public static partial class DatabaseContextFactory
    {
        #region Views

        internal static IEnumerable<Schema.View> MapView(IEnumerable<FlatView> sources, IReadOnlyDictionary<int, IReadOnlyList<Schema.IColumn>> columnsByObjectId)
        {
            return sources
                .GroupBy(v => v.ObjectId)
                .Select(group =>
                {
                    var definition = group.First();
                    columnsByObjectId.TryGetValue(definition.ObjectId, out var viewColumns);

                    return new Schema.View
                    {
                        ObjectId = definition.ObjectId,
                        DatabaseName = definition.DatabaseName,
                        SchemaName = definition.SchemaName,
                        Name = definition.DefinitionName,
                        FullName = $"{definition.SchemaName}.{definition.Name}",
                        Definition = definition.Definition,

                        // Project from canonical columns instead of instantiating new duplicates
                        Columns = viewColumns ?? Array.Empty<Schema.IColumn>()
                    };
                });
        }

        #endregion
    }
}
