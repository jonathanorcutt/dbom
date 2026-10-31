using JEO3.Engine.Models;
using JEO3.Providers;
using JEO3.Schema;

namespace JEO3.Engine
{
    public static partial class DatabaseContextFactory
    {
        #region User Defined Types

        internal static IEnumerable<UserDefinedType> MapUserDefinedType(IEnumerable<FlatUserDefinedType> sources, IEnumerable<IColumn> columns)
        {
            return sources
                .GroupBy(udt => udt.ObjectId)
                .Select(group =>
                {
                    var header = group.First();
                    var typeKind = header.FunctionType switch
                    {
                        "ALIAS_UDT" => UserDefinedTypeKind.Alias,
                        "TABLE_TYPE" => UserDefinedTypeKind.Structured,
                        "CLR_UDT" => UserDefinedTypeKind.ClrType,
                        _ => UserDefinedTypeKind.Unknown
                    };
                    return new UserDefinedType
                    {
                        ObjectId = header.ObjectId,
                        DatabaseName = header.DatabaseName,
                        SchemaName = header.SchemaName,
                        Name = header.DefinitionName, // Or header.DefinitionName depending on property name
                        FullName = header.FullName,
                        TypeKind = typeKind,
                        BaseType = header.DataType,
                        ProviderBaseType = header.ProviderDataType,
                        MaxLength = header.MaxLength,
                        Precision = header.Precision,
                        Scale = header.Scale,
                        CreateDate = header.CreateDate,
                        ModifyDate = header.ModifyDate,
                        IsSystemObject = header.IsSystemObject,
                        Definition = header.Definition,
                        DefinitionHash = header.DefinitionHash,
                        SchemaId = header.SchemaId,
                        IsNullable = header.IsNullable,
                        Identifier = header.Identifier,
                        IsDeterministic = header.IsDeterministic,
                        IsReplicated = header.IsReplicated,
                        WithCheckOption = header.WithCheckOption,

                        // Map columns directly from the collapsed group (for Table Types)
                        Columns = group
                            .Where(u => !string.IsNullOrEmpty(u.Name)) // Guard for scalar UDTs that have no table columns
                            .OrderBy(u => u.OrdinalPosition)
                            .Select(u => new UserDefinedTypeColumn
                            {
                                OrdinalPosition = u.OrdinalPosition,
                                Name = u.Name,
                                DataType = u.DataType,
                                ProviderDataType = u.ProviderDataType,
                                MaximumLength = u.MaxLength,
                                Precision = u.Precision,
                                Scale = u.Scale,
                                IsNullable = u.IsNullable,
                                DefaultValue = u.DefaultValue
                            })
                            .Cast<IUserDefinedTypeColumn>()
                            .ToList()
                    };
                });
        }

        #endregion
    }
}
