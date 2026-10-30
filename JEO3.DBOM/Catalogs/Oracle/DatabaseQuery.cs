namespace JEO3.Providers.Catalogs
{
    internal static partial class OracleQueries
    {
        public const string DatabaseQuery = @"
SELECT
    d.DBID AS ObjectId,
    d.NAME AS Name,
    d.NAME AS DatabaseName,
    CAST(NULL AS NUMBER) AS CompatibilityLevel,
    CAST(NULL AS NUMBER(1)) AS IsReadCommitted,
    d.CREATED AS CreateDate,
    CAST(NULL AS VARCHAR2(128)) AS CollationName
FROM V$DATABASE d";
    }
}
