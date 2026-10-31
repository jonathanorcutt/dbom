using JEO3.Engine.Models;
using JEO3.Providers;
using JEO3.Schema;

namespace JEO3.Engine
{
    public static partial class DatabaseContextFactory
    {
        #region Procedures

        internal static IEnumerable<Procedure> MapProcedure(IEnumerable<FlatStoredProcedure> sources)
        {
            return sources
                .GroupBy(p => p.ObjectId)
                .Select(group =>
                {
                    var header = group.First();

                    return new Procedure
                    {
                        ObjectId = header.ObjectId,
                        DatabaseName = header.DatabaseName,
                        SchemaName = header.SchemaName,
                        Name = header.DefinitionName, // Or header.DefinitionName depending on property name
                        FullName = header.FullName,
                        Identifier = header.Identifier,
                        Definition = header.Definition,

                        // Map parameters directly from the collapsed group
                        Parameters = group
                            .Where(p => !string.IsNullOrEmpty(p.Name)) // Guard against definitions with zero parameters
                            .OrderBy(p => p.OrdinalPosition)
                            .Select(p => new ProcedureParameter
                            {
                                Ordinal = p.OrdinalPosition,
                                Name = p.Name,
                                DataType = p.DataType,
                                ProviderDataType = p.ProviderDataType,
                                MaxLength = p.MaxLength,
                                Precision = p.Precision,
                                Scale = p.Scale,
                                IsOutput = p.IsOutput,
                                IsNullable = p.IsNullable,
                                //IsReadOnly = p.IsReadOnly,
                                DefaultValue = p.DefaultValue
                            })
                            .ToList()
                    };
                });
        }

        #endregion
    }
}
