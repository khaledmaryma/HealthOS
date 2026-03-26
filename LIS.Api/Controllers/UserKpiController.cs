using System.Data;
using LIS.Api.Data;
using LIS.Api.Models.UserManagement;
using LIS.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace LIS.Api.Controllers
{
    [ApiController]
    [Route("api/user-kpis")]
    public class UserKpiController : ControllerBase
    {
        private readonly HISUsersDbContext _usersDb;
        private readonly IConfiguration _configuration;
        private readonly ILogger<UserKpiController> _logger;

        public UserKpiController(
            HISUsersDbContext usersDb,
            IConfiguration configuration,
            ILogger<UserKpiController> logger)
        {
            _usersDb = usersDb;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<UserKpiResponseDto>>> List(
            [FromQuery] int userId,
            [FromQuery] string appKey,
            [FromQuery] string homePageId = "main",
            CancellationToken cancellationToken = default)
        {
            if (userId <= 0 || string.IsNullOrWhiteSpace(appKey))
                return BadRequest("userId and appKey are required.");

            var rows = await _usersDb.UserKpiDefinitions
                .AsNoTracking()
                .Where(k => k.UserId == userId && k.AppKey == appKey && k.HomePageId == homePageId && !k.IsDeleted)
                .OrderBy(k => k.SortOrder)
                .ThenBy(k => k.Id)
                .Select(k => new UserKpiResponseDto(
                    k.Id,
                    k.UserId,
                    k.AppKey,
                    k.HomePageId,
                    k.Title,
                    k.SqlQuery,
                    k.DisplayMode,
                    k.GridShowTotals,
                    k.ChartOptionsJson,
                    k.SortOrder,
                    k.CreatedUtc,
                    k.ModifiedUtc))
                .ToListAsync(cancellationToken);

            return Ok(rows);
        }

        [HttpPost]
        public async Task<ActionResult<UserKpiResponseDto>> Create([FromBody] UserKpiCreateDto dto, CancellationToken cancellationToken)
        {
            var err = KpiSqlGuard.ValidateReadOnlySelect(dto.SqlQuery);
            if (err != null)
                return BadRequest(err);

            var entity = new UserKpiDefinition
            {
                UserId = dto.UserId,
                AppKey = dto.AppKey.Trim(),
                HomePageId = string.IsNullOrWhiteSpace(dto.HomePageId) ? "main" : dto.HomePageId.Trim(),
                Title = dto.Title.Trim(),
                SqlQuery = dto.SqlQuery.Trim(),
                DisplayMode = dto.DisplayMode,
                GridShowTotals = dto.GridShowTotals,
                ChartOptionsJson = string.IsNullOrWhiteSpace(dto.ChartOptionsJson) ? null : dto.ChartOptionsJson.Trim(),
                SortOrder = dto.SortOrder,
                CreatedUtc = DateTime.UtcNow,
                IsDeleted = false
            };

            _usersDb.UserKpiDefinitions.Add(entity);
            await _usersDb.SaveChangesAsync(cancellationToken);

            return Ok(ToDto(entity));
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<UserKpiResponseDto>> Update(
            int id,
            [FromBody] UserKpiUpdateDto dto,
            CancellationToken cancellationToken)
        {
            var err = KpiSqlGuard.ValidateReadOnlySelect(dto.SqlQuery);
            if (err != null)
                return BadRequest(err);

            var entity = await _usersDb.UserKpiDefinitions
                .FirstOrDefaultAsync(k => k.Id == id && !k.IsDeleted, cancellationToken);

            if (entity == null)
                return NotFound();

            if (entity.UserId != dto.UserId)
                return BadRequest("KPI does not belong to this user.");

            entity.AppKey = dto.AppKey.Trim();
            entity.HomePageId = string.IsNullOrWhiteSpace(dto.HomePageId) ? "main" : dto.HomePageId.Trim();
            entity.Title = dto.Title.Trim();
            entity.SqlQuery = dto.SqlQuery.Trim();
            entity.DisplayMode = dto.DisplayMode;
            entity.GridShowTotals = dto.GridShowTotals;
            entity.ChartOptionsJson = string.IsNullOrWhiteSpace(dto.ChartOptionsJson) ? null : dto.ChartOptionsJson.Trim();
            entity.SortOrder = dto.SortOrder;
            entity.ModifiedUtc = DateTime.UtcNow;

            await _usersDb.SaveChangesAsync(cancellationToken);
            return Ok(ToDto(entity));
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id, [FromQuery] int userId, CancellationToken cancellationToken = default)
        {
            var entity = await _usersDb.UserKpiDefinitions
                .FirstOrDefaultAsync(k => k.Id == id && !k.IsDeleted, cancellationToken);

            if (entity == null)
                return NotFound();

            if (entity.UserId != userId)
                return BadRequest("KPI does not belong to this user.");

            entity.IsDeleted = true;
            entity.ModifiedUtc = DateTime.UtcNow;
            await _usersDb.SaveChangesAsync(cancellationToken);
            return NoContent();
        }

        /// <summary>
        /// Execute a KPI query: either by saved KPI id (recommended) or ad-hoc SQL for preview (same userId).
        /// </summary>
        [HttpPost("execute")]
        public async Task<ActionResult<KpiQueryResultDto>> Execute(
            [FromBody] ExecuteKpiQueryRequest request,
            CancellationToken cancellationToken)
        {
            if (request.UserId <= 0)
                return BadRequest("userId is required.");

            string sql;
            if (request.KpiId is > 0)
            {
                var kpi = await _usersDb.UserKpiDefinitions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(k => k.Id == request.KpiId && !k.IsDeleted, cancellationToken);

                if (kpi == null)
                    return NotFound("KPI not found.");

                if (kpi.UserId != request.UserId)
                    return BadRequest("KPI does not belong to this user.");

                sql = kpi.SqlQuery;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(request.Sql))
                    return BadRequest("sql is required when kpiId is not set.");

                sql = request.Sql!;
            }

            var validationError = KpiSqlGuard.ValidateReadOnlySelect(sql);
            if (validationError != null)
                return BadRequest(validationError);

            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
                return StatusCode(500, "Server configuration error: DefaultConnection is missing.");

            try
            {
                await using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync(cancellationToken);

                await using var cmd = conn.CreateCommand();
                cmd.CommandText = sql;
                cmd.CommandType = CommandType.Text;
                cmd.CommandTimeout = 30;

                await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

                var columns = new List<string>();
                for (var i = 0; i < reader.FieldCount; i++)
                    columns.Add(reader.GetName(i));

                var rows = new List<Dictionary<string, object?>>();
                var count = 0;
                while (await reader.ReadAsync(cancellationToken))
                {
                    count++;
                    if (count > KpiSqlGuard.MaxRows)
                        return BadRequest($"Result exceeds maximum of {KpiSqlGuard.MaxRows} rows.");

                    var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                    for (var i = 0; i < reader.FieldCount; i++)
                    {
                        var name = reader.GetName(i);
                        row[name] = await reader.IsDBNullAsync(i, cancellationToken) ? null : reader.GetValue(i);
                    }

                    rows.Add(row);
                }

                return Ok(new KpiQueryResultDto(columns, rows));
            }
            catch (SqlException ex)
            {
                _logger.LogWarning(ex, "KPI SQL execution failed");
                return BadRequest($"SQL error: {ex.Message}");
            }
        }

        private static UserKpiResponseDto ToDto(UserKpiDefinition k) =>
            new(
                k.Id,
                k.UserId,
                k.AppKey,
                k.HomePageId,
                k.Title,
                k.SqlQuery,
                k.DisplayMode,
                k.GridShowTotals,
                k.ChartOptionsJson,
                k.SortOrder,
                k.CreatedUtc,
                k.ModifiedUtc);

        public sealed record UserKpiResponseDto(
            int Id,
            int UserId,
            string AppKey,
            string HomePageId,
            string Title,
            string SqlQuery,
            int DisplayMode,
            bool GridShowTotals,
            string? ChartOptionsJson,
            int SortOrder,
            DateTime CreatedUtc,
            DateTime? ModifiedUtc);

        public sealed record UserKpiCreateDto(
            int UserId,
            string AppKey,
            string HomePageId,
            string Title,
            string SqlQuery,
            int DisplayMode,
            bool GridShowTotals,
            string? ChartOptionsJson,
            int SortOrder);

        public sealed record UserKpiUpdateDto(
            int UserId,
            string AppKey,
            string HomePageId,
            string Title,
            string SqlQuery,
            int DisplayMode,
            bool GridShowTotals,
            string? ChartOptionsJson,
            int SortOrder);

        public sealed record ExecuteKpiQueryRequest(int UserId, int? KpiId, string? Sql);

        public sealed record KpiQueryResultDto(IReadOnlyList<string> Columns, IReadOnlyList<Dictionary<string, object?>> Rows);
    }
}
