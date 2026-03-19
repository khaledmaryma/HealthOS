using System.Data;
using System.Data.Common;
using LIS.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace LIS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountingController : ControllerBase
    {
        private readonly BillingDbContext _billingDb;
        private readonly IConfiguration _configuration;

        public AccountingController(BillingDbContext billingDb, IConfiguration configuration)
        {
            _billingDb = billingDb;
            _configuration = configuration;
        }

        // create an explicit connection to the financial database (uses separate string or swaps DB name)
        private DbConnection GetFinancialConnection()
        {
            var explicitConn = _configuration.GetConnectionString("FinancialConnection");
            if (!string.IsNullOrWhiteSpace(explicitConn))
            {
                return new SqlConnection(explicitConn);
            }
            var billingConn = _billingDb.Database.GetDbConnection() as SqlConnection;
            if (billingConn == null)
            {
                throw new InvalidOperationException("Billing db connection is not SqlConnection");
            }
            var builder = new SqlConnectionStringBuilder(billingConn.ConnectionString);
            var dbName = _configuration["FinancialDatabaseName"];
            if (!string.IsNullOrWhiteSpace(dbName)) builder.InitialCatalog = dbName;
            return new SqlConnection(builder.ToString());
        }

        public class DepartmentCashDto
        {
            public string Department { get; set; } = string.Empty;
            public decimal LBP { get; set; }
            public decimal USD { get; set; }
        }

        public class AccountIncomeDto
        {
            public string AccountCode { get; set; } = string.Empty;
            public string AccountDescription { get; set; } = string.Empty;
            public decimal LBP { get; set; }
            public decimal USD { get; set; }
        }

        public class AccountNode
        {
            public int Id { get; set; }
            public string? Code { get; set; }
            public string? Description { get; set; }
            public int? Currency { get; set; }
            public decimal DebitLocal { get; set; }
            public decimal CreditLocal { get; set; }
            public decimal DebitMain { get; set; }
            public decimal CreditMain { get; set; }
            public List<AccountNode>? Children { get; set; }
        }

        public class AccountStatementFilter
        {
            public string? FromDate { get; set; }
            public string? ToDate { get; set; }
            public bool? IsDueDate { get; set; }
            public string? AccountCode { get; set; }
            public int? JobId { get; set; }
            public int? GroupId { get; set; }
            public int[]? VoucherTypeIds { get; set; }
            public int? AccountCurrencyId { get; set; }
            public string? Comment { get; set; }
        }

        public class AccountStatementRow
        {
            public string? VoucherNumber { get; set; }
            public string? VoucherDate { get; set; }
            public string? AccountCode { get; set; }
            public string? AccountDescription { get; set; }
            public decimal? DebitLocal { get; set; }
            public decimal? CreditLocal { get; set; }
            public decimal? DebitMain { get; set; }
            public decimal? CreditMain { get; set; }
            public string? Comments { get; set; }
        }

        private sealed class RawRow
        {
            public string Dep { get; set; } = string.Empty;
            public string? Comment { get; set; }
            public decimal Recived { get; set; }
            public int CurrencyID { get; set; }
            public string? AdmissionNB { get; set; }
            public int BeautyCount { get; set; }
        }

        [HttpGet("DailyCashByDepartment")]
        public async Task<ActionResult<IEnumerable<DepartmentCashDto>>> GetDailyCashByDepartment()
        {
            // Execute the provided SQL against the Billing database
            var rows = new List<RawRow>();

            await using var conn = GetFinancialConnection();
            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync();

            var financialDbName = _configuration["FinancialDatabaseName"] ?? "Financial DB";
            var fin = $"[{financialDbName}]";
            string BuildQuery(string cashierDbQualifier) => $@"select  LEFT(cd.Comment,1) as dep, cd.Comment, cc.Recived, cc.CurrencyID, AdmissionNB,

            (select count(*) from Billing.dbo.InvoiceHeader as invh 
            inner join Billing.dbo.InvoiceDetail as invd on invh.ID = invd.InvoiceHeader
            where invh.IsDeleted = 0 and invd.IsDeleted = 0 and invh.AdmissionNumber = cd.AdmissionNB and invd.CostCenter = 54) as beautycount from {cashierDbQualifier}.dbo.CashierHeader as ch 

            inner join {cashierDbQualifier}.dbo.CashierDetail as cd on ch.ID = cd.CashierHeaderID

            inner join {cashierDbQualifier}.dbo.CashCollectDetail as cc on cd.ID = cc.CashierDetailID

            where ch.IsDeleted = 0 and cd.IsDeleted = 0 and cc.IsDeleted = 0 and ch.CloseDate is null and cc.PaymentType <> 4";

            async Task LoadRowsAsync(string dbQualifier)
            {
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = BuildQuery(dbQualifier);
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    rows.Add(new RawRow
                    {
                        Dep = reader.GetString(0),
                        Comment = reader.IsDBNull(1) ? null : reader.GetString(1),
                        Recived = reader.IsDBNull(2) ? 0m : reader.GetDecimal(2),
                        CurrencyID = reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
                        AdmissionNB = reader.IsDBNull(4) ? null : reader.GetString(4),
                        BeautyCount = reader.IsDBNull(5) ? 0 : reader.GetInt32(5)
                    });
                }
            }

            // First try Financial DB, then fallback to Billing if no data
            await LoadRowsAsync(fin);
            if (rows.Count == 0)
            {
                rows.Clear();
                await LoadRowsAsync("Billing");
            }

            // Transform rows into department aggregates with LBP/USD
            var aggregates = new Dictionary<string, (decimal lbp, decimal usd)>(StringComparer.OrdinalIgnoreCase);

            foreach (var r in rows)
            {
                var department = MapDepartment(r);
                if (string.IsNullOrEmpty(department))
                    continue;

                if (!aggregates.TryGetValue(department, out var agg))
                    agg = (0m, 0m);

                if (r.CurrencyID == 1)
                    agg.lbp += r.Recived;
                else if (r.CurrencyID == 2)
                    agg.usd += r.Recived;

                aggregates[department] = agg;
            }

            var result = aggregates
                .OrderBy(kv => kv.Key)
                .Select(kv => new DepartmentCashDto
                {
                    Department = kv.Key,
                    LBP = kv.Value.lbp,
                    USD = kv.Value.usd
                })
                .ToList();

            return Ok(result);
        }

        private static string MapDepartment(RawRow r)
        {
            var dep = (r.Dep ?? string.Empty).Trim().ToUpperInvariant();

            if (dep == "C")
            {
                if (r.BeautyCount > 0)
                    return "Beauty";
                if (string.IsNullOrWhiteSpace(r.AdmissionNB))
                    return "Med Supp";
                return "Clinic";
            }

            if (dep == "L")
                return "Labo";
            if (dep == "R")
                return "Radio";

            return string.Empty;
        }

        [HttpGet("CurrentMonthIncomeByAccount")]
        public async Task<ActionResult<IEnumerable<AccountIncomeDto>>> GetCurrentMonthIncomeByAccount()
        {
            var rows = new List<AccountIncomeDto>();
            await using var conn = GetFinancialConnection();
            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync();

            var financialDbName = _configuration["FinancialDatabaseName"] ?? "Financial DB";
            var fin = $"[{financialDbName}]";

            // Get current year and month
            var now = DateTime.Now;
            var currentYear = now.Year;
            var currentMonth = now.Month;

            async Task LoadAsync(string dbQualifier)
            {
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = $@"select vd.AccountCode, vd.AccountDescription,
                SUM(case when vd.AccountCurreny = 1 then vd.DbLocal else 0 end) as [In LL],
                sum(case when vd.AccountCurreny = 2 then vd.DbMain else 0 end) [In USD]
                from {dbQualifier}.dbo.VoucherHeader as vh inner join {dbQualifier}.dbo.VoucherDetail as vd on vh.ID = vd.VoucherHeaderID
                where vh.IsDeleted = 0 and vd.IsDeleted = 0 and vd.AccountCode in (
                  select acc.Code from {dbQualifier}.dbo.Account  as acc where acc.IsAccessible = 1 and acc.IsDeleted = 0 and acc.JobID = 6
                )
                and Year(vh.VoucherDate) = {currentYear} and MONTH(vh.VoucherDate) = {currentMonth}
                group by vd.AccountCode, vd.AccountDescription";
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    rows.Add(new AccountIncomeDto
                    {
                        AccountCode = reader.IsDBNull(0) ? string.Empty : reader.GetString(0),
                        AccountDescription = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                        LBP = reader.IsDBNull(2) ? 0m : reader.GetDecimal(2),
                        USD = reader.IsDBNull(3) ? 0m : reader.GetDecimal(3)
                    });
                }
            }

            try
            {
                await LoadAsync(fin);
            }
            catch
            {
                // ignore and try fallback
            }

            if (rows.Count == 0)
            {
                rows.Clear();
                try { await LoadAsync("Billing"); } catch { }
            }

            Console.WriteLine($"Account statement query returned {rows.Count} rows");
            return Ok(rows);
        }

        [HttpPost("account-statement")]
        public async Task<ActionResult<IEnumerable<AccountStatementRow>>> GetAccountStatement([FromBody] AccountStatementFilter filter)
        {
            if (filter == null)
            {
                return BadRequest(new { message = "Filter is required" });
            }

            var rows = new List<AccountStatementRow>();
            await using var conn = GetFinancialConnection();
            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync();

            var financialDbName = _configuration["FinancialDatabaseName"] ?? "Financial DB";
            var fin = $"[{financialDbName}]";

            // Build the SQL query dynamically based on filter
            var whereClauses = new List<string>();
            var parameters = new List<SqlParameter>();

            if (!string.IsNullOrWhiteSpace(filter.FromDate))
            {
                whereClauses.Add("vh.VoucherDate >= @FromDate");
                parameters.Add(new SqlParameter("@FromDate", DateTime.Parse(filter.FromDate)));
            }

            if (!string.IsNullOrWhiteSpace(filter.ToDate))
            {
                whereClauses.Add("vh.VoucherDate <= @ToDate");
                parameters.Add(new SqlParameter("@ToDate", DateTime.Parse(filter.ToDate)));
            }

            if (filter.IsDueDate == true)
            {
                whereClauses.Add("vh.VoucherDueDate IS NOT NULL");
            }

            if (!string.IsNullOrWhiteSpace(filter.AccountCode))
            {
                whereClauses.Add("vd.AccountCode = @AccountCode");
                parameters.Add(new SqlParameter("@AccountCode", filter.AccountCode));
            }

            if (filter.JobId.HasValue)
            {
                whereClauses.Add("a.JobID = @JobId");
                parameters.Add(new SqlParameter("@JobId", filter.JobId.Value));
            }

            if (filter.GroupId.HasValue)
            {
                whereClauses.Add("a.GroupID = @GroupId");
                parameters.Add(new SqlParameter("@GroupId", filter.GroupId.Value));
            }

            if (filter.VoucherTypeIds != null && filter.VoucherTypeIds.Length > 0)
            {
                whereClauses.Add($"vh.VoucherTypeID IN ({string.Join(",", filter.VoucherTypeIds)})");
            }

            if (filter.AccountCurrencyId.HasValue)
            {
                whereClauses.Add("vd.AccountCurreny = @AccountCurrencyId");
                parameters.Add(new SqlParameter("@AccountCurrencyId", filter.AccountCurrencyId.Value));
            }

            if (!string.IsNullOrWhiteSpace(filter.Comment))
            {
                whereClauses.Add("vd.Comment LIKE @Comment");
                parameters.Add(new SqlParameter("@Comment", $"%{filter.Comment}%"));
            }

            var whereClause = whereClauses.Count > 0 ? "AND " + string.Join(" AND ", whereClauses) : "";

            // Build the query - only join Account table if JobID or GroupID filters are used
            string sql;
            if (filter.JobId.HasValue || filter.GroupId.HasValue)
            {
                sql = $@"SELECT vh.VoucherNumber, vh.VoucherDate, vd.AccountCode, vd.AccountDescription,
                        vd.DbLocal as DebitLocal, vd.CrLocal as CreditLocal,
                        vd.DbMain as DebitMain, vd.CrMain as CreditMain, vd.Comments as Comments
                        FROM {fin}.dbo.VoucherHeader vh
                        INNER JOIN {fin}.dbo.VoucherDetail vd ON vh.ID = vd.VoucherHeaderID
                        INNER JOIN {fin}.dbo.Account a ON vd.AccountCode = a.Code AND a.IsDeleted = 0
                        WHERE vh.IsDeleted = 0 AND vd.IsDeleted = 0 {whereClause}
                        ORDER BY vh.VoucherDate, vh.VoucherNumber";
            }
            else
            {
                sql = $@"SELECT vh.VoucherNumber, vh.VoucherDate, vd.AccountCode, vd.AccountDescription,
                        vd.DbLocal as DebitLocal, vd.CrLocal as CreditLocal,
                        vd.DbMain as DebitMain, vd.CrMain as CreditMain, vd.Comments as Comments
                        FROM {fin}.dbo.VoucherHeader vh
                        INNER JOIN {fin}.dbo.VoucherDetail vd ON vh.ID = vd.VoucherHeaderID
                        WHERE vh.IsDeleted = 0 AND vd.IsDeleted = 0 {whereClause}
                        ORDER BY vh.VoucherDate, vh.VoucherNumber";
            }

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.AddRange(parameters.ToArray());

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var row = new AccountStatementRow
                {
                    VoucherNumber = reader.IsDBNull(0) ? null : reader.GetString(0),
                    VoucherDate = reader.IsDBNull(1) ? null : reader.GetDateTime(1).ToString("yyyy-MM-dd"),
                    AccountCode = reader.IsDBNull(2) ? null : reader.GetString(2),
                    AccountDescription = reader.IsDBNull(3) ? null : reader.GetString(3),
                    DebitLocal = reader.IsDBNull(4) ? null : (decimal?)reader.GetDecimal(4),
                    CreditLocal = reader.IsDBNull(5) ? null : (decimal?)reader.GetDecimal(5),
                    DebitMain = reader.IsDBNull(6) ? null : (decimal?)reader.GetDecimal(6),
                    CreditMain = reader.IsDBNull(7) ? null : (decimal?)reader.GetDecimal(7),
                    Comments = reader.IsDBNull(8) ? null : reader.GetString(8)
                };
                rows.Add(row);
            }

            Console.WriteLine($"Account statement query returned {rows.Count} rows");
            return Ok(rows);
        }

        public class TrialBalanceFilter
        {
            public string? FromDate { get; set; }
            public string? ToDate { get; set; }
            public bool ExcludeOpeningClosing { get; set; } = false;
        }

        [HttpGet("trial-balance")]
        public async Task<IActionResult> GetTrialBalance()
        {
            // Default to current year for GET requests
            var filter = new TrialBalanceFilter
            {
                FromDate = $"{DateTime.Now.Year}-01-01",
                ToDate = $"{DateTime.Now.Year}-12-31",
                ExcludeOpeningClosing = false
            };

            return await GetTrialBalance(filter);
        }

        [HttpPost("trial-balance")]
        public async Task<IActionResult> GetTrialBalance([FromBody] TrialBalanceFilter? filter = null)
        {
            try
            {
                filter ??= new TrialBalanceFilter();

                // Default to current year if no dates provided
                if (string.IsNullOrWhiteSpace(filter.FromDate) && string.IsNullOrWhiteSpace(filter.ToDate))
                {
                    var currentYear = DateTime.Now.Year;
                    filter.FromDate = $"{currentYear}-01-01";
                    filter.ToDate = $"{currentYear}-12-31";
                }

                await using var conn = GetFinancialConnection();
                await conn.OpenAsync();

                var financialDbName = _configuration["FinancialDatabaseName"] ?? "Financial DB";
                var fin = $"[{financialDbName}]";

                // Build where clause for date filtering and voucher type exclusion
                var whereClauses = new List<string>();
                var parameters = new List<SqlParameter>();

                if (!string.IsNullOrWhiteSpace(filter.FromDate))
                {
                    whereClauses.Add("vh.VoucherDate >= @FromDate");
                    parameters.Add(new SqlParameter("@FromDate", DateTime.Parse(filter.FromDate)));
                }

                if (!string.IsNullOrWhiteSpace(filter.ToDate))
                {
                    whereClauses.Add("vh.VoucherDate <= @ToDate");
                    parameters.Add(new SqlParameter("@ToDate", DateTime.Parse(filter.ToDate)));
                }

                if (filter.ExcludeOpeningClosing)
                {
                    whereClauses.Add("vh.VoucherTypeID NOT IN (17, 18)");
                }

                var whereClause = whereClauses.Count > 0 ? "AND " + string.Join(" AND ", whereClauses) : "";

                var sql = $@"
                    SELECT 
                        vd.AccountCode as Code,
                        MAX(vd.AccountDescription) as Description,
                        MAX(a.CurrencyID) as Currency,
                        COALESCE(SUM(vd.DbLocal), 0) as DebitLocal,
                        COALESCE(SUM(vd.CrLocal), 0) as CreditLocal,
                        COALESCE(SUM(vd.DbMain), 0) as DebitMain,
                        COALESCE(SUM(vd.CrMain), 0) as CreditMain
                    FROM {fin}.dbo.VoucherDetail vd
                    INNER JOIN {fin}.dbo.VoucherHeader vh ON vd.VoucherHeaderID = vh.ID AND vh.IsDeleted = 0
                    LEFT JOIN {fin}.dbo.Account a ON vd.AccountCode = a.Code AND a.IsDeleted = 0
                    WHERE vd.IsDeleted = 0 {whereClause}
                    GROUP BY vd.AccountCode
                    ORDER BY vd.AccountCode";

                Console.WriteLine($"Executing trial balance query with database: {fin}");
                Console.WriteLine($"Filter - FromDate: {filter.FromDate}, ToDate: {filter.ToDate}, ExcludeOpeningClosing: {filter.ExcludeOpeningClosing}");
                Console.WriteLine($"SQL: {sql}");

                await using var cmd = conn.CreateCommand();
                cmd.CommandText = sql;
                cmd.Parameters.AddRange(parameters.ToArray());

                var accounts = new List<AccountNode>();

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var account = new AccountNode
                    {
                        Id = 0, // We don't have ID from this query
                        Code = reader.IsDBNull(0) ? null : reader.GetString(0),
                        Description = reader.IsDBNull(1) ? null : reader.GetString(1),
                        Currency = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                        DebitLocal = reader.IsDBNull(3) ? 0 : reader.GetDecimal(3),
                        CreditLocal = reader.IsDBNull(4) ? 0 : reader.GetDecimal(4),
                        DebitMain = reader.IsDBNull(5) ? 0 : reader.GetDecimal(5),
                        CreditMain = reader.IsDBNull(6) ? 0 : reader.GetDecimal(6)
                    };
                    accounts.Add(account);
                }

                Console.WriteLine($"Trial balance query returned {accounts.Count} accounts");
                return Ok(accounts);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetTrialBalance: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }
    }
}


