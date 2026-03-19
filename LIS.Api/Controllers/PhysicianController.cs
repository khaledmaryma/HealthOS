using LIS.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LIS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PhysicianController : ControllerBase
    {
        private readonly LISDbContext _context;
        private readonly ILogger<PhysicianController> _logger;

        public PhysicianController(LISDbContext context, ILogger<PhysicianController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Search physicians by name
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<object>>> Search([FromQuery] string query)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return BadRequest("Search query cannot be empty");
                }

                var results = new List<object>();
                
                using var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();
                
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT TOP 50 ID, Name
                    FROM HospitalDefinition.dbo.Physician
                    WHERE IsDeleted = 0 AND Name LIKE @query
                    ORDER BY Name";
                
                var queryParam = command.CreateParameter();
                queryParam.ParameterName = "@query";
                queryParam.Value = $"%{query}%";
                command.Parameters.Add(queryParam);
                
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    results.Add(new
                    {
                        id = reader.GetInt32(0),
                        name = reader.IsDBNull(1) ? "" : reader.GetString(1),
                        specialty = ""
                    });
                }

                _logger.LogInformation("Search for '{Query}' returned {Count} physicians", query, results.Count);
                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching physicians with query '{Query}'", query);
                return StatusCode(500, "An error occurred while searching physicians");
            }
        }
    }
}
