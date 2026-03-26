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

        /// <summary>Cashier detail row for open (CloseDate is null) cashier headers.</summary>
        public class CashierDetailRowDto
        {
            public int? DailyCounter { get; set; }
            public int? CashierDetailCounter { get; set; }
            public string? Department { get; set; }
            public string? DistributionTypeDescription { get; set; }
            public string? PayedBY { get; set; }
            public string? MouvementNb { get; set; }
            public int? VoucherNumber { get; set; }
            public decimal? AmoutToBePayed { get; set; }
            public int? AccountCurrency { get; set; }
            public decimal? CollectionLBP { get; set; }
            public decimal? CollectionUSD { get; set; }
            public decimal? DifferenceUSD { get; set; }
            public decimal? DifferenceLBP { get; set; }
        }

        /// <summary>Summary by department for open cashier.</summary>
        public class CashierDepartmentSummaryDto
        {
            public string Department { get; set; } = string.Empty;
            public decimal CollectionLBP { get; set; }
            public decimal CollectionUSD { get; set; }
        }

        public class CloseCashierRequest
        {
            public string? NewOpenDate { get; set; }
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

        /// <summary>Get cashier details for open cashier headers (CloseDate is null). Includes main grid data and department summary.</summary>
        [HttpGet("CashierOpenDetails")]
        public async Task<ActionResult<object>> GetCashierOpenDetails()
        {
            await using var conn = GetFinancialConnection();
            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync();

            var financialDbName = _configuration["FinancialDatabaseName"] ?? "Financial DB";
            var fin = $"[{financialDbName}]";

            var details = new List<CashierDetailRowDto>();
            var summary = new List<CashierDepartmentSummaryDto>();
            DateTime? openDate = null;
            int? cashierHeaderId = null;

            var openHeaderSql = $@"SELECT TOP 1 ch.ID, ch.OpenDate FROM {fin}.dbo.CashierHeader ch WHERE ISNULL(ch.IsDeleted, 0) = 0 AND ch.CloseDate IS NULL ORDER BY ch.ID DESC";
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = openHeaderSql;
                await using var rdr = await cmd.ExecuteReaderAsync();
                if (await rdr.ReadAsync())
                {
                    cashierHeaderId = rdr.IsDBNull(0) ? null : rdr.GetInt32(0);
                    openDate = rdr.IsDBNull(1) ? null : rdr.GetDateTime(1);
                }
            }

            var sqlWithLookup = $@"
                SELECT 
                    cd.DailyCounter,
                    cd.CashierDetailCounter,
                    cd.Department,
                    dt.[Description] AS DistributionTypeDescription,
                    cd.comment as PayedBY,
                    cd.MouvementNb,
                    cd.VoucherNumber,
                    cd.AmoutToBePayed,
                    cd.AccountCurrency,
                    cd.CollectionLBP,
                    cd.CollectionUSD,
                    cd.DifferenceUSD,
                    cd.DifferenceLBP
                FROM {fin}.dbo.CashierHeader ch
                INNER JOIN {fin}.dbo.CashierDetail cd ON ch.ID = cd.CashierHeaderID AND ISNULL(cd.IsDeleted, 0) = 0
                LEFT JOIN {fin}.dbo.DistrubutionTypes dt ON cd.DistributionTypesID = dt.ID AND ISNULL(dt.IsDeleted, 0) = 0
                WHERE ISNULL(ch.IsDeleted, 0) = 0 AND ch.CloseDate IS NULL
                ORDER BY cd.DailyCounter, cd.CashierDetailCounter";

            var sqlWithoutLookup = $@"
                SELECT 
                    cd.DailyCounter,
                    cd.CashierDetailCounter,
                    cd.Department,
                    CAST(NULL AS NVARCHAR(255)) AS DistributionTypeDescription,
                    cd.comment as PayedBY,
                    cd.MouvementNb,
                    cd.VoucherNumber,
                    cd.AmoutToBePayed,
                    cd.AccountCurrency,
                    cd.CollectionLBP,
                    cd.CollectionUSD,
                    cd.DifferenceUSD,
                    cd.DifferenceLBP
                FROM {fin}.dbo.CashierHeader ch
                INNER JOIN {fin}.dbo.CashierDetail cd ON ch.ID = cd.CashierHeaderID AND ISNULL(cd.IsDeleted, 0) = 0
                WHERE ISNULL(ch.IsDeleted, 0) = 0 AND ch.CloseDate IS NULL
                ORDER BY cd.DailyCounter, cd.CashierDetailCounter";

            var sql = sqlWithLookup;
            try
            {
                await using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = sql;
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    details.Add(new CashierDetailRowDto
                    {
                        DailyCounter = reader.IsDBNull(0) ? null : reader.GetInt32(0),
                        CashierDetailCounter = reader.IsDBNull(1) ? null : reader.GetInt32(1),
                        Department = reader.IsDBNull(2) ? null : reader.GetString(2),
                        DistributionTypeDescription = reader.IsDBNull(3) ? null : reader.GetString(3),
                        PayedBY = reader.IsDBNull(4) ? null : reader.GetString(4),
                        MouvementNb = reader.IsDBNull(5) ? null : reader.GetString(5),
                        VoucherNumber = reader.IsDBNull(6) ? null : reader.GetInt32(6),
                        AmoutToBePayed = reader.IsDBNull(7) ? null : reader.GetDecimal(7),
                        AccountCurrency = reader.IsDBNull(8) ? 2 : reader.GetInt32(8),
                        CollectionLBP = reader.IsDBNull(9) ? null : reader.GetDecimal(9),
                        CollectionUSD = reader.IsDBNull(10) ? null : reader.GetDecimal(10),
                        DifferenceUSD = reader.IsDBNull(11) ? null : reader.GetDecimal(11),
                        DifferenceLBP = reader.IsDBNull(12) ? null : reader.GetDecimal(12)
                    });
                }
                }
            }
            catch (SqlException ex) when (ex.Number == 208 || ex.Message.Contains("Invalid object name"))
            {
                details.Clear();
                sql = sqlWithoutLookup;
                await using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = sql;
                    await using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        details.Add(new CashierDetailRowDto
                        {
                            DailyCounter = reader.IsDBNull(0) ? null : reader.GetInt32(0),
                            CashierDetailCounter = reader.IsDBNull(1) ? null : reader.GetInt32(1),
                            Department = reader.IsDBNull(2) ? null : reader.GetString(2),
                            DistributionTypeDescription = null,
                            PayedBY = reader.IsDBNull(4) ? null : reader.GetString(4),
                            MouvementNb = reader.IsDBNull(5) ? null : reader.GetString(5),
                            VoucherNumber = reader.IsDBNull(6) ? null : reader.GetInt32(6),
                            AmoutToBePayed = reader.IsDBNull(7) ? null : reader.GetDecimal(7),
                            AccountCurrency = reader.IsDBNull(8) ? 2 : reader.GetInt32(8),
                            CollectionLBP = reader.IsDBNull(9) ? null : reader.GetDecimal(9),
                            CollectionUSD = reader.IsDBNull(10) ? null : reader.GetDecimal(10),
                            DifferenceUSD = reader.IsDBNull(11) ? null : reader.GetDecimal(11),
                            DifferenceLBP = reader.IsDBNull(12) ? null : reader.GetDecimal(12)
                        });
                    }
                }
            }

            var summarySql = $@"
                SELECT 
                    ISNULL(cd.Department, '(Blank)') AS Department,
                    ISNULL(SUM(cd.CollectionLBP), 0) AS CollectionLBP,
                    ISNULL(SUM(cd.CollectionUSD), 0) AS CollectionUSD
                FROM {fin}.dbo.CashierHeader ch
                INNER JOIN {fin}.dbo.CashierDetail cd ON ch.ID = cd.CashierHeaderID AND ISNULL(cd.IsDeleted, 0) = 0
                WHERE ISNULL(ch.IsDeleted, 0) = 0 AND ch.CloseDate IS NULL
                GROUP BY cd.Department
                ORDER BY cd.Department";

            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = summarySql;
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    summary.Add(new CashierDepartmentSummaryDto
                    {
                        Department = reader.IsDBNull(0) ? "(Blank)" : reader.GetString(0),
                        CollectionLBP = reader.IsDBNull(1) ? 0m : reader.GetDecimal(1),
                        CollectionUSD = reader.IsDBNull(2) ? 0m : reader.GetDecimal(2)
                    });
                }
            }

            var totalLBP = details.Sum(d => d.CollectionLBP ?? 0);
            var totalUSD = details.Sum(d => d.CollectionUSD ?? 0);

            return Ok(new
            {
                cashierHeaderId,
                openDate = openDate?.ToString("yyyy-MM-dd"),
                details,
                summary,
                totals = new { totalLBP, totalUSD }
            });
        }

        /// <summary>Get cashier data for printing. draft=true: current open; draft=false: last closed.</summary>
        [HttpGet("CashierForPrint")]
        public async Task<ActionResult<object>> GetCashierForPrint([FromQuery] bool draft = true)
        {
            await using var conn = GetFinancialConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();
            var fin = $"[{_configuration["FinancialDatabaseName"] ?? "Financial DB"}]";

            string whereClause = draft
                ? "ch.CloseDate IS NULL"
                : "ch.CloseDate IS NOT NULL";
            string orderClause = draft ? "ch.ID DESC" : "ch.CloseDate DESC, ch.ID DESC";

            var headerSql = $@"SELECT TOP 1 ch.ID, ch.OpenDate, ch.CloseDate, ch.CloseTime FROM {fin}.dbo.CashierHeader ch WHERE ISNULL(ch.IsDeleted, 0) = 0 AND {whereClause} ORDER BY {orderClause}";
            int? headerId = null;
            DateTime? hOpenDate = null;
            DateTime? hCloseDate = null;
            TimeSpan? hCloseTime = null;
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = headerSql;
                await using var rdr = await cmd.ExecuteReaderAsync();
                if (await rdr.ReadAsync())
                {
                    headerId = rdr.IsDBNull(0) ? null : rdr.GetInt32(0);
                    hOpenDate = rdr.IsDBNull(1) ? null : rdr.GetDateTime(1);
                    hCloseDate = rdr.IsDBNull(2) ? null : rdr.GetDateTime(2);
                    hCloseTime = rdr.IsDBNull(3) ? default(TimeSpan?) : rdr.GetFieldValue<TimeSpan>(3);
                }
            }
            if (!headerId.HasValue)
                return Ok(new { openDate = (string?)null, closeDate = (string?)null, details = Array.Empty<CashierDetailRowDto>(), summary = Array.Empty<CashierDepartmentSummaryDto>(), totals = new { totalLBP = 0m, totalUSD = 0m } });

            var detailSql = $@"
                SELECT cd.DailyCounter, cd.CashierDetailCounter, cd.Department, dt.[Description], cd.comment, cd.MouvementNb, cd.VoucherNumber, cd.AmoutToBePayed, cd.AccountCurrency, cd.CollectionLBP, cd.CollectionUSD, cd.DifferenceUSD, cd.DifferenceLBP
                FROM {fin}.dbo.CashierDetail cd
                LEFT JOIN {fin}.dbo.DistrubutionTypes dt ON cd.DistributionTypesID = dt.ID
                WHERE cd.CashierHeaderID = @Hid AND ISNULL(cd.IsDeleted, 0) = 0
                ORDER BY cd.DailyCounter, cd.CashierDetailCounter";
            var detailList = new List<CashierDetailRowDto>();
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = detailSql;
                cmd.Parameters.Add(new SqlParameter("@Hid", headerId.Value));
                await using var rdr = await cmd.ExecuteReaderAsync();
                while (await rdr.ReadAsync())
                {
                    detailList.Add(new CashierDetailRowDto
                    {
                        DailyCounter = rdr.IsDBNull(0) ? null : rdr.GetInt32(0),
                        CashierDetailCounter = rdr.IsDBNull(1) ? null : rdr.GetInt32(1),
                        Department = rdr.IsDBNull(2) ? null : rdr.GetString(2),
                        DistributionTypeDescription = rdr.IsDBNull(3) ? null : rdr.GetString(3),
                        PayedBY = rdr.IsDBNull(4) ? null : rdr.GetString(4),
                        MouvementNb = rdr.IsDBNull(5) ? null : rdr.GetString(5),
                        VoucherNumber = rdr.IsDBNull(6) ? null : rdr.GetInt32(6),
                        AmoutToBePayed = rdr.IsDBNull(7) ? null : rdr.GetDecimal(7),
                        AccountCurrency = rdr.IsDBNull(8) ? 2 : rdr.GetInt32(8),
                        CollectionLBP = rdr.IsDBNull(9) ? null : rdr.GetDecimal(9),
                        CollectionUSD = rdr.IsDBNull(10) ? null : rdr.GetDecimal(10),
                        DifferenceUSD = rdr.IsDBNull(11) ? null : rdr.GetDecimal(11),
                        DifferenceLBP = rdr.IsDBNull(12) ? null : rdr.GetDecimal(12)
                    });
                }
            }

            var sumSql = $@"SELECT ISNULL(cd.Department, '(Blank)') AS Department, ISNULL(SUM(cd.CollectionLBP), 0) AS CollectionLBP, ISNULL(SUM(cd.CollectionUSD), 0) AS CollectionUSD FROM {fin}.dbo.CashierDetail cd WHERE cd.CashierHeaderID = @Hid2 AND ISNULL(cd.IsDeleted, 0) = 0 GROUP BY cd.Department ORDER BY cd.Department";
            var sumList = new List<CashierDepartmentSummaryDto>();
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = sumSql;
                cmd.Parameters.Add(new SqlParameter("@Hid2", headerId.Value));
                await using var rdr = await cmd.ExecuteReaderAsync();
                while (await rdr.ReadAsync())
                    sumList.Add(new CashierDepartmentSummaryDto { Department = rdr.GetString(0), CollectionLBP = rdr.GetDecimal(1), CollectionUSD = rdr.GetDecimal(2) });
            }
            var tLBP = detailList.Sum(d => d.CollectionLBP ?? 0);
            var tUSD = detailList.Sum(d => d.CollectionUSD ?? 0);
            return Ok(new { cashierHeaderId = headerId, openDate = hOpenDate?.ToString("yyyy-MM-dd"), closeDate = hCloseDate?.ToString("yyyy-MM-dd"), closeTime = hCloseTime?.ToString(@"hh\:mm"), details = detailList, summary = sumList, totals = new { totalLBP = tLBP, totalUSD = tUSD } });
        }

        /// <summary>Close current cashier and open a new one with the given OpenDate.</summary>
        [HttpPost("CloseCashierAndOpenNew")]
        public async Task<ActionResult<object>> CloseCashierAndOpenNew([FromBody] CloseCashierRequest req)
        {
            if (string.IsNullOrEmpty(req?.NewOpenDate))
                return BadRequest(new { message = "NewOpenDate is required (yyyy-MM-dd)" });

            await using var conn = GetFinancialConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();
            var fin = $"[{_configuration["FinancialDatabaseName"] ?? "Financial DB"}]";

            var getOpenSql = $@"SELECT TOP 1 ch.ID, ch.CashierID, ch.Number FROM {fin}.dbo.CashierHeader ch WHERE ISNULL(ch.IsDeleted, 0) = 0 AND ch.CloseDate IS NULL ORDER BY ch.ID DESC";
            int? currentId = null;
            int? cashierId = null;
            int? currentNumber = null;
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = getOpenSql;
                await using var rdr = await cmd.ExecuteReaderAsync();
                if (await rdr.ReadAsync())
                {
                    currentId = rdr.GetInt32(0);
                    cashierId = rdr.IsDBNull(1) ? 1 : rdr.GetInt32(1);
                    currentNumber = rdr.IsDBNull(2) ? 1 : rdr.GetInt32(2);
                }
            }
            if (!currentId.HasValue)
                return BadRequest(new { message = "No open cashier found" });

            var sumSql = $@"SELECT ISNULL(SUM(CollectionLBP), 0), ISNULL(SUM(CollectionUSD), 0) FROM {fin}.dbo.CashierDetail WHERE CashierHeaderID = @Hid AND ISNULL(IsDeleted, 0) = 0";
            decimal totalLBP = 0, totalUSD = 0;
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = sumSql;
                cmd.Parameters.Add(new SqlParameter("@Hid", currentId.Value));
                await using var rdr = await cmd.ExecuteReaderAsync();
                if (await rdr.ReadAsync()) { totalLBP = rdr.GetDecimal(0); totalUSD = rdr.GetDecimal(1); }
            }

            var now = DateTime.Now;
            var newOpenDate = DateTime.Parse(req.NewOpenDate);

            await using var tx = conn.BeginTransaction();
            try
            {
                var updateSql = $@"UPDATE {fin}.dbo.CashierHeader SET CloseDate = @CloseDate, CloseTime = @CloseTime, TotalCashLocal = @TotalLBP, TotalCashMain = @TotalUSD, ModifiedBy = 338, ModifiedDate = GETDATE() WHERE ID = @Id";
                await using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = updateSql;
                    cmd.Parameters.Add(new SqlParameter("@CloseDate", now.Date));
                    cmd.Parameters.Add(new SqlParameter("@CloseTime", now.TimeOfDay));
                    cmd.Parameters.Add(new SqlParameter("@TotalLBP", totalLBP));
                    cmd.Parameters.Add(new SqlParameter("@TotalUSD", totalUSD));
                    cmd.Parameters.Add(new SqlParameter("@Id", currentId.Value));
                    await cmd.ExecuteNonQueryAsync();
                }

                var nextNum = (currentNumber ?? 0) + 1;
                var insertSql = $@"INSERT INTO {fin}.dbo.CashierHeader (Number, CashierID, OpenDate, OpenTime, CloseDate, CloseTime, IsDeleted, CreatedBy, CreatedDate) VALUES (@Num, @CashierId, @OpenDate, @OpenTime, NULL, NULL, 0, 338, GETDATE())";
                await using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = insertSql;
                    cmd.Parameters.Add(new SqlParameter("@Num", nextNum));
                    cmd.Parameters.Add(new SqlParameter("@CashierId", cashierId ?? 1));
                    cmd.Parameters.Add(new SqlParameter("@OpenDate", newOpenDate.Date));
                    cmd.Parameters.Add(new SqlParameter("@OpenTime", newOpenDate.TimeOfDay));
                    await cmd.ExecuteNonQueryAsync();
                }
                await tx.CommitAsync();
                return Ok(new { message = "Cashier closed and new opened", newOpenDate = newOpenDate.ToString("yyyy-MM-dd") });
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
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


