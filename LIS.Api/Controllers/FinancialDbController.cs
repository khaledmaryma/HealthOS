using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using LIS.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace LIS.Api.Controllers
{
    [ApiController]
    [Route("api/accounting/financial")]
    public class FinancialDbController : ControllerBase
    {
        private readonly BillingDbContext _billingDb;
        private readonly IConfiguration _configuration;

        public FinancialDbController(BillingDbContext billingDb, IConfiguration configuration)
        {
            _billingDb = billingDb;
            _configuration = configuration;
        }

        [HttpGet("accounts")]
        public async Task<ActionResult<IEnumerable<Dictionary<string, object?>>>> GetAccounts([FromQuery] int top = 200, [FromQuery] bool accessibleOnly = false)
        {
            var sql = BuildTopQuery("Account", top, "ID");
            var rows = await QueryAsync(sql, new SqlParameter("@top", ClampTop(top)));
            if (!accessibleOnly)
            {
                return Ok(rows);
            }

            var filtered = rows.Where(row =>
            {
                if (!row.TryGetValue("IsAccessible", out var value) || value == null)
                {
                    return false;
                }
                return Convert.ToInt32(value) == 1;
            }).ToList();

            return Ok(filtered);
        }

        [HttpGet("voucher-headers")]
        public async Task<ActionResult<IEnumerable<Dictionary<string, object?>>>> GetVoucherHeaders([FromQuery] int top = 200)
        {
            var sql = BuildTopQuery("VoucherHeader", top, "ID");
            return Ok(await QueryAsync(sql, new SqlParameter("@top", ClampTop(top))));
        }

        [HttpGet("voucher-details")]
        public async Task<ActionResult<IEnumerable<Dictionary<string, object?>>>> GetVoucherDetails([FromQuery] int voucherHeaderId, [FromQuery] int top = 500)
        {
            if (voucherHeaderId <= 0)
            {
                return BadRequest(new { message = "voucherHeaderId is required" });
            }

            var sql = BuildDetailQuery("VoucherDetail", "VoucherHeaderID", "ID");
            return Ok(await QueryAsync(
                sql,
                new SqlParameter("@top", ClampTop(top)),
                new SqlParameter("@parentId", voucherHeaderId)
            ));
        }

        [HttpGet("voucher-types")]
        public async Task<ActionResult<IEnumerable<Dictionary<string, object?>>>> GetVoucherTypes([FromQuery] int top = 200)
        {
            var sql = BuildTopQuery("VoucherType", top, "ID");
            return Ok(await QueryAsync(sql, new SqlParameter("@top", ClampTop(top))));
        }

        [HttpGet("cashier-headers")]
        public async Task<ActionResult<IEnumerable<Dictionary<string, object?>>>> GetCashierHeaders([FromQuery] int top = 200)
        {
            var sql = BuildTopQuery("CashierHeader", top, "ID");
            return Ok(await QueryAsync(sql, new SqlParameter("@top", ClampTop(top))));
        }

        [HttpGet("cashier-details")]
        public async Task<ActionResult<IEnumerable<Dictionary<string, object?>>>> GetCashierDetails([FromQuery] int cashierHeaderId, [FromQuery] int top = 500)
        {
            if (cashierHeaderId <= 0)
            {
                return BadRequest(new { message = "cashierHeaderId is required" });
            }

            var sql = BuildDetailQuery("CashierDetail", "CashierHeaderID", "ID");
            return Ok(await QueryAsync(
                sql,
                new SqlParameter("@top", ClampTop(top)),
                new SqlParameter("@parentId", cashierHeaderId)
            ));
        }

        [HttpGet("cashier-detail-distributions")]
        public async Task<ActionResult<IEnumerable<Dictionary<string, object?>>>> GetCashierDetailDistributions([FromQuery] int cashierDetailId, [FromQuery] int top = 500)
        {
            if (cashierDetailId <= 0)
            {
                return BadRequest(new { message = "cashierDetailId is required" });
            }

            var sql = BuildDetailQuery("CashierDetailDistribution", "CashierDetailID", "ID");
            return Ok(await QueryAsync(
                sql,
                new SqlParameter("@top", ClampTop(top)),
                new SqlParameter("@parentId", cashierDetailId)
            ));
        }

        [HttpGet("cash-collect-details")]
        public async Task<ActionResult<IEnumerable<Dictionary<string, object?>>>> GetCashCollectDetails([FromQuery] int cashierDetailId, [FromQuery] int top = 500)
        {
            if (cashierDetailId <= 0)
            {
                return BadRequest(new { message = "cashierDetailId is required" });
            }

            var sql = BuildDetailQuery("CashCollectDetail", "CashierDetailID", "ID");
            return Ok(await QueryAsync(
                sql,
                new SqlParameter("@top", ClampTop(top)),
                new SqlParameter("@parentId", cashierDetailId)
            ));
        }

        [HttpGet("configuration")]
        public async Task<ActionResult<IEnumerable<Dictionary<string, object?>>>> GetConfiguration([FromQuery] int top = 200)
        {
            var sql = BuildTopQuery("ConfigurationTable", top, "ID");
            return Ok(await QueryAsync(sql, new SqlParameter("@top", ClampTop(top))));
        }

        [HttpPut("configuration/{id:int}")]
        public async Task<ActionResult> UpdateConfiguration(int id, [FromBody] Dictionary<string, object?> input)
        {
            if (input == null || input.Count == 0)
            {
                return BadRequest(new { message = "Payload is required" });
            }

            await using var conn = _billingDb.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open)
            {
                await conn.OpenAsync();
            }

            await using var tx = conn.BeginTransaction();
            try
            {
                var columns = await GetColumnsAsync(conn, tx, "ConfigurationTable");
                var data = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                foreach (var kvp in input)
                {
                    if (string.Equals(kvp.Key, "ID", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    if (columns.Contains(kvp.Key))
                    {
                        data[kvp.Key] = kvp.Value ?? DBNull.Value;
                    }
                }

                if (data.Count == 0)
                {
                    return BadRequest(new { message = "No valid columns to update" });
                }

                var (sql, parameters) = BuildUpdate("ConfigurationTable", data, "ID");
                await using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = sql;
                foreach (var p in parameters)
                {
                    cmd.Parameters.Add(p);
                }
                cmd.Parameters.Add(new SqlParameter("@id", id));
                await cmd.ExecuteNonQueryAsync();
                await tx.CommitAsync();
                return NoContent();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        [HttpPost("vouchers")]
        public async Task<ActionResult> CreateVoucher([FromBody] VoucherSaveRequest request)
        {
            if (request.Header == null)
            {
                return BadRequest(new { message = "Header is required" });
            }
            if (request.Details == null || request.Details.Count == 0)
            {
                return BadRequest(new { message = "At least one voucher detail is required" });
            }

            await using var conn = _billingDb.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open)
            {
                await conn.OpenAsync();
            }

            await using var tx = conn.BeginTransaction();
            try
            {
                var headerId = await InsertVoucherHeaderAsync(conn, tx, request.Header);
                await InsertVoucherDetailsAsync(conn, tx, headerId, request.Details);
                await tx.CommitAsync();
                return Ok(new { id = headerId });
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        [HttpPut("vouchers/{id:int}")]
        public async Task<ActionResult> UpdateVoucher(int id, [FromBody] VoucherSaveRequest request)
        {
            if (request.Header == null)
            {
                return BadRequest(new { message = "Header is required" });
            }
            if (request.Details == null)
            {
                return BadRequest(new { message = "Details are required" });
            }

            await using var conn = _billingDb.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open)
            {
                await conn.OpenAsync();
            }

            await using var tx = conn.BeginTransaction();
            try
            {
                await UpdateVoucherHeaderAsync(conn, tx, id, request.Header);
                await ClearVoucherDetailsAsync(conn, tx, id);
                if (request.Details.Count > 0)
                {
                    await InsertVoucherDetailsAsync(conn, tx, id, request.Details);
                }
                await tx.CommitAsync();
                return NoContent();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        [HttpDelete("vouchers/{id:int}")]
        public async Task<ActionResult> DeleteVoucher(int id)
        {
            await using var conn = _billingDb.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open)
            {
                await conn.OpenAsync();
            }

            await using var tx = conn.BeginTransaction();
            try
            {
                await MarkVoucherDeletedAsync(conn, tx, id);
                await ClearVoucherDetailsAsync(conn, tx, id);
                await tx.CommitAsync();
                return NoContent();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        private string BuildTopQuery(string tableName, int top, string orderColumn)
        {
            var fin = BuildFinancialQualifier();
            return $@"
SELECT TOP (@top) *
FROM {fin}.dbo.{tableName}
WHERE ISNULL(IsDeleted, 0) = 0
ORDER BY {orderColumn} DESC";
        }

        private string BuildDetailQuery(string tableName, string parentColumn, string orderColumn)
        {
            var fin = BuildFinancialQualifier();
            return $@"
SELECT TOP (@top) *
FROM {fin}.dbo.{tableName}
WHERE ISNULL(IsDeleted, 0) = 0
  AND {parentColumn} = @parentId
ORDER BY {orderColumn} ASC";
        }

        private string BuildFinancialQualifier()
        {
            var dbName = _configuration["FinancialDatabaseName"] ?? "Financial DB";
            return $"[{dbName}]";
        }

        private static int ClampTop(int top)
        {
            if (top <= 0) return 200;
            return Math.Min(top, 2000);
        }

        private async Task<List<Dictionary<string, object?>>> QueryAsync(string sql, params SqlParameter[] parameters)
        {
            var rows = new List<Dictionary<string, object?>>();
            await using var conn = _billingDb.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open)
            {
                await conn.OpenAsync();
            }

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            foreach (var parameter in parameters)
            {
                cmd.Parameters.Add(parameter);
            }

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                }
                rows.Add(row);
            }

            return rows;
        }

        private static readonly ConcurrentDictionary<string, HashSet<string>> ColumnCache = new(StringComparer.OrdinalIgnoreCase);

        private async Task<HashSet<string>> GetColumnsAsync(DbConnection conn, DbTransaction tx, string tableName)
        {
            var key = tableName.ToLowerInvariant();
            if (ColumnCache.TryGetValue(key, out var cached))
            {
                return cached;
            }

            var fin = BuildFinancialQualifier();
            var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            await using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = $@"
SELECT COLUMN_NAME
FROM {fin}.INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = @tableName";
            cmd.Parameters.Add(new SqlParameter("@tableName", tableName));
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                columns.Add(reader.GetString(0));
            }

            ColumnCache[key] = columns;
            return columns;
        }

        private async Task<int> InsertVoucherHeaderAsync(DbConnection conn, DbTransaction tx, VoucherHeaderDto header)
        {
            var columns = await GetColumnsAsync(conn, tx, "VoucherHeader");
            var data = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            AddIfExists(columns, data, "VoucherDate", header.VoucherDate);
            AddIfExists(columns, data, "VoucherDueDate", header.VoucherDueDate);
            AddIfExists(columns, data, "DueDate", header.VoucherDueDate);
            AddIfExists(columns, data, "VoucherTypeID", header.VoucherTypeId);
            AddIfExists(columns, data, "VoucherTypeId", header.VoucherTypeId);
            AddIfExists(columns, data, "VoucherNumber", header.VoucherNumber);
            AddIfExists(columns, data, "Number", header.VoucherNumber);
            AddIfExists(columns, data, "Comments", header.Comment);
            AddIfExists(columns, data, "Comment", header.Comment);
            AddIfExists(columns, data, "IsDeleted", 0);
            AddIfExists(columns, data, "CreatedDate", DateTime.UtcNow);
            AddIfExists(columns, data, "CreatedBy", -1);

            var (sql, parameters) = BuildInsert("VoucherHeader", data);
            await using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = sql + "; SELECT CAST(SCOPE_IDENTITY() as int);";
            foreach (var p in parameters)
            {
                cmd.Parameters.Add(p);
            }
            var result = await cmd.ExecuteScalarAsync();
            return result == null ? 0 : Convert.ToInt32(result);
        }

        private async Task UpdateVoucherHeaderAsync(DbConnection conn, DbTransaction tx, int id, VoucherHeaderDto header)
        {
            var columns = await GetColumnsAsync(conn, tx, "VoucherHeader");
            var data = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            AddIfExists(columns, data, "VoucherDate", header.VoucherDate);
            AddIfExists(columns, data, "VoucherDueDate", header.VoucherDueDate);
            AddIfExists(columns, data, "DueDate", header.VoucherDueDate);
            AddIfExists(columns, data, "VoucherTypeID", header.VoucherTypeId);
            AddIfExists(columns, data, "VoucherTypeId", header.VoucherTypeId);
            AddIfExists(columns, data, "VoucherNumber", header.VoucherNumber);
            AddIfExists(columns, data, "Number", header.VoucherNumber);
            AddIfExists(columns, data, "Comments", header.Comment);
            AddIfExists(columns, data, "Comment", header.Comment);
            AddIfExists(columns, data, "ModifiedDate", DateTime.UtcNow);
            AddIfExists(columns, data, "ModifiedBy", -1);

            if (data.Count == 0)
            {
                return;
            }

            var (sql, parameters) = BuildUpdate("VoucherHeader", data, "ID");
            await using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = sql;
            foreach (var p in parameters)
            {
                cmd.Parameters.Add(p);
            }
            cmd.Parameters.Add(new SqlParameter("@id", id));
            await cmd.ExecuteNonQueryAsync();
        }

        private async Task InsertVoucherDetailsAsync(DbConnection conn, DbTransaction tx, int headerId, List<VoucherDetailDto> details)
        {
            var columns = await GetColumnsAsync(conn, tx, "VoucherDetail");
            foreach (var detail in details)
            {
                var data = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                AddIfExists(columns, data, "VoucherHeaderID", headerId);
                AddIfExists(columns, data, "AccountID", detail.AccountId);
                AddIfExists(columns, data, "AccountId", detail.AccountId);
                AddIfExists(columns, data, "AccountCode", detail.AccountCode);
                AddIfExists(columns, data, "AccountCurreny", detail.AccountCurrencyId);
                AddIfExists(columns, data, "AccountCurrency", detail.AccountCurrencyId);
                AddIfExists(columns, data, "AccountDescription", detail.AccountDescription);
                AddIfExists(columns, data, "DbLocal", detail.DebitLocal);
                AddIfExists(columns, data, "DbMain", detail.DebitMain);
                AddIfExists(columns, data, "CrLocal", detail.CreditLocal);
                AddIfExists(columns, data, "CrMain", detail.CreditMain);
                AddIfExists(columns, data, "Rate", detail.Rate);
                AddIfExists(columns, data, "DebitLocal", detail.DebitLocal);
                AddIfExists(columns, data, "DebitMain", detail.DebitMain);
                AddIfExists(columns, data, "CreditLocal", detail.CreditLocal);
                AddIfExists(columns, data, "CreditMain", detail.CreditMain);
                AddIfExists(columns, data, "Comments", detail.Comment);
                AddIfExists(columns, data, "Comment", detail.Comment);
                AddIfExists(columns, data, "CostCenter", detail.CostCenter);
                AddIfExists(columns, data, "CostCenterID", detail.CostCenter);
                AddIfExists(columns, data, "IsDeleted", 0);
                AddIfExists(columns, data, "CreatedDate", DateTime.UtcNow);
                AddIfExists(columns, data, "CreatedBy", -1);

                var (sql, parameters) = BuildInsert("VoucherDetail", data);
                await using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = sql;
                foreach (var p in parameters)
                {
                    cmd.Parameters.Add(p);
                }
                await cmd.ExecuteNonQueryAsync();
            }
        }

        private async Task ClearVoucherDetailsAsync(DbConnection conn, DbTransaction tx, int headerId)
        {
            var columns = await GetColumnsAsync(conn, tx, "VoucherDetail");
            if (columns.Contains("IsDeleted"))
            {
                await using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = $"UPDATE {BuildFinancialQualifier()}.dbo.VoucherDetail SET IsDeleted = 1 WHERE VoucherHeaderID = @headerId";
                cmd.Parameters.Add(new SqlParameter("@headerId", headerId));
                await cmd.ExecuteNonQueryAsync();
                return;
            }

            await using var deleteCmd = conn.CreateCommand();
            deleteCmd.Transaction = tx;
            deleteCmd.CommandText = $"DELETE FROM {BuildFinancialQualifier()}.dbo.VoucherDetail WHERE VoucherHeaderID = @headerId";
            deleteCmd.Parameters.Add(new SqlParameter("@headerId", headerId));
            await deleteCmd.ExecuteNonQueryAsync();
        }

        private async Task MarkVoucherDeletedAsync(DbConnection conn, DbTransaction tx, int headerId)
        {
            var columns = await GetColumnsAsync(conn, tx, "VoucherHeader");
            if (!columns.Contains("IsDeleted"))
            {
                return;
            }

            await using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = $"UPDATE {BuildFinancialQualifier()}.dbo.VoucherHeader SET IsDeleted = 1 WHERE ID = @id";
            cmd.Parameters.Add(new SqlParameter("@id", headerId));
            await cmd.ExecuteNonQueryAsync();
        }

        private static void AddIfExists(HashSet<string> columns, Dictionary<string, object?> data, string column, object? value)
        {
            if (!columns.Contains(column))
            {
                return;
            }
            data[column] = value ?? DBNull.Value;
        }

        private (string sql, List<SqlParameter> parameters) BuildInsert(string tableName, Dictionary<string, object?> data)
        {
            var fin = BuildFinancialQualifier();
            var columns = data.Keys.ToList();
            var parameterNames = columns.Select((_, index) => $"@p{index}").ToList();
            var sql = $@"
                INSERT INTO {fin}.dbo.{tableName} ({string.Join(", ", columns)})
                VALUES ({string.Join(", ", parameterNames)})";
                            var parameters = columns
                .Select((column, index) => new SqlParameter(parameterNames[index], data[column] ?? DBNull.Value))
                .ToList();
            return (sql, parameters);
        }

        private (string sql, List<SqlParameter> parameters) BuildUpdate(string tableName, Dictionary<string, object?> data, string keyColumn)
        {
            var fin = BuildFinancialQualifier();
            var columns = data.Keys.ToList();
            var assignments = columns.Select((column, index) => $"{column} = @p{index}").ToList();
            var sql = $@"
UPDATE {fin}.dbo.{tableName}
SET {string.Join(", ", assignments)}
WHERE {keyColumn} = @id";
            var parameters = columns
                .Select((column, index) => new SqlParameter($"@p{index}", data[column] ?? DBNull.Value))
                .ToList();
            return (sql, parameters);
        }

        public sealed class VoucherSaveRequest
        {
            public VoucherHeaderDto? Header { get; set; }
            public List<VoucherDetailDto> Details { get; set; } = new();
        }

        public sealed class VoucherHeaderDto
        {
            public int? Id { get; set; }
            public DateTime VoucherDate { get; set; }
            public DateTime? VoucherDueDate { get; set; }
            public int VoucherTypeId { get; set; }
            public string? VoucherNumber { get; set; }
            public string? Comment { get; set; }
        }

        public sealed class VoucherDetailDto
        {
            public int? Id { get; set; }
            public int AccountId { get; set; }
            public string? AccountCode { get; set; }
            public int? AccountCurrencyId { get; set; }
            public string? AccountDescription { get; set; }
            public decimal Rate { get; set; }
            public decimal DebitLocal { get; set; }
            public decimal DebitMain { get; set; }
            public decimal CreditLocal { get; set; }
            public decimal CreditMain { get; set; }
            public string? Comment { get; set; }
            public string? CostCenter { get; set; }
        }
    }
}
