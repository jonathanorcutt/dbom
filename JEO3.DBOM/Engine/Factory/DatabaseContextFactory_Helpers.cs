using JEO3.Engine.Models;
using JEO3.Providers;
using JEO3.Schema;

namespace JEO3.Engine
{
    public static partial class DatabaseContextFactory
    {
        #region Helpers

        private static string TableKey(string? schema, string? table)
        {
            return $"{schema ?? string.Empty}.{table ?? string.Empty}";
        }

        #endregion
    }
}
