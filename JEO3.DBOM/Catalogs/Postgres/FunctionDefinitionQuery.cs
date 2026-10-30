namespace JEO3.Providers.Catalogs
{
    internal static partial class PostgresQueries
    {
        internal const string FunctionDefinitionQuery = @"
SELECT 
    p.oid::integer AS ObjectId,
    
    -- FunctionType & ObjectType
    CASE p.prorettype
        WHEN 'pg_catalog.trigger'::regtype THEN 'TRIGGER_FUNCTION'
        WHEN 'pg_catalog.record'::regtype THEN 'TABLE_VALUED_FUNCTION'
        ELSE 'SCALAR_FUNCTION'
    END AS FunctionType,
    'FUNCTION' AS ObjectType,
    -- Definition (reconstructs the full CREATE OR REPLACE FUNCTION SQL block)
    pg_get_functiondef(p.oid) AS Definition,
    format_type(p.prorettype, NULL) AS ReturnType,
    format_type(p.prorettype, NULL) AS ProviderReturnType,
    (n.nspname IN ('pg_catalog', 'information_schema')) AS IsSystemObject,
    (p.provolatile = 'i') AS IsDeterministic,
    concat('""', n.nspname, '"".""', p.proname, '""') AS Identifier,
    current_database() AS DatabaseName,
    n.nspname AS SchemaName,
    p.proname AS Name,
    concat(current_database(), '.', n.nspname, '.', p.proname) AS FullName
FROM pg_proc p
JOIN pg_namespace n ON n.oid = p.pronamespace
WHERE n.nspname NOT IN ('pg_catalog', 'information_schema')
  AND p.prokind = 'f'; -- 'f' for functions ('p' for procedures, 'a' for aggregates, 'w' for window functions)
";
    }
}
