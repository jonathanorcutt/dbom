using JEO3.Engine.Models;
using JEO3.Providers;
using JEO3.Schema;

namespace JEO3.Engine
{
    public static partial class DatabaseContextFactory
    {
        #region Functions

        internal static IEnumerable<Schema.Function> MapFunction(IEnumerable<FlatFunction> sources)
        {
            return sources
                .GroupBy(f => f.ObjectId)
                .Select(group =>
                {
                    var header = group.First();

                    return new Schema.Function
                    {
                        ObjectId = header.ObjectId,
                        DatabaseName = header.DatabaseName,
                        SchemaName = header.SchemaName,
                        Name = header.DefinitionName, // Or header.DefinitionName depending on property
                        FullName = header.FullName,
                        Definition = header.Definition,
                        ReturnType = header.ReturnType,
                        ProviderReturnType = header.ProviderReturnType,

                        // Map parameters directly from the collapsed group
                        Parameters = group
                            .Where(f => !string.IsNullOrEmpty(f.Name)) // Guard against functions with zero parameters
                            .OrderBy(f => f.OrdinalPosition)
                            .Select(f => new ProcedureParameter
                            {
                                Ordinal = f.OrdinalPosition,
                                Name = f.Name,
                                DataType = f.DataType,
                                ProviderDataType = f.ProviderDataType,
                                MaxLength = f.MaxLength,
                                Precision = f.Precision,
                                Scale = f.Scale,
                                IsOutput = f.IsOutput,
                                IsNullable = f.IsNullable,
                                DefaultValue = f.DefaultValue
                            })
                            .ToList()
                    };
                });
        }

        #endregion
    }
}
