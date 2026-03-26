using System.Text.RegularExpressions;

namespace LIS.Api.Services
{
    /// <summary>
    /// Read-only SELECT validation for user-defined KPI SQL. Not a substitute for DB permissions.
    /// </summary>
    public static class KpiSqlGuard
    {
        public const int MaxSqlLength = 8000;
        public const int MaxRows = 5000;

        private static readonly string[] ForbiddenTokens =
        {
            "INSERT", "UPDATE", "DELETE", "DROP", "ALTER", "CREATE", "TRUNCATE",
            "MERGE", "GRANT", "REVOKE", "EXEC", "EXECUTE", "OPENROWSET", "OPENQUERY",
            "BULK", "SHUTDOWN", "BACKUP", "RESTORE", "KILL", "DBCC", "WAITFOR"
        };

        /// <summary>Returns null if valid; otherwise an error message.</summary>
        public static string? ValidateReadOnlySelect(string? sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
                return "Query is empty.";

            var trimmed = sql.Trim();
            if (trimmed.Length > MaxSqlLength)
                return $"Query exceeds maximum length ({MaxSqlLength} characters).";

            var statements = trimmed.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (statements.Length != 1)
                return "Only a single SQL statement is allowed (no multiple batches).";

            var body = statements[0];
            var upper = body.ToUpperInvariant();

            if (!upper.StartsWith("SELECT", StringComparison.Ordinal) && !upper.StartsWith("WITH", StringComparison.Ordinal))
                return "Query must start with SELECT or WITH (read-only).";

            foreach (var token in ForbiddenTokens)
            {
                if (ContainsSqlToken(upper, token))
                    return $"The keyword '{token}' is not allowed in KPI queries.";
            }

            if (upper.Contains("XP_", StringComparison.Ordinal))
                return "Extended procedures are not allowed in KPI queries.";

            if (Regex.IsMatch(body, @"--|/\*", RegexOptions.None))
                return "Comments (-- or /*) are not allowed in KPI queries.";

            // Reduce risk of SELECT INTO new table
            if (Regex.IsMatch(upper, @"\bSELECT\b[\s\S]*\bINTO\b"))
                return "SELECT ... INTO is not allowed.";

            return null;
        }

        private static bool ContainsSqlToken(string sqlUpper, string token)
        {
            var pattern = $@"\b{Regex.Escape(token)}\b";
            return Regex.IsMatch(sqlUpper, pattern);
        }
    }
}
