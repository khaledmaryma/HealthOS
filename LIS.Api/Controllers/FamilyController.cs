using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace LIS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FamilyController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<FamilyController> _logger;

        public FamilyController(IConfiguration configuration, ILogger<FamilyController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<object>>> Search([FromQuery] string? query = null)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection")
                    ?.Replace("Database=LIS", "Database=CommonDefinition");
                
                var families = new List<object>();

                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var whereClause = "IsDeleted = 0";
                    if (!string.IsNullOrWhiteSpace(query))
                    {
                        whereClause += " AND Name LIKE @query";
                    }

                    var sql = $@"
                        SELECT TOP 50 ID, Name, NameA as ArabicName
                        FROM Family WITH (NOLOCK)
                        WHERE {whereClause}
                        ORDER BY Name";

                    using (var command = new SqlCommand(sql, connection))
                    {
                        if (!string.IsNullOrWhiteSpace(query))
                        {
                            command.Parameters.AddWithValue("@query", $"%{query}%");
                        }

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                families.Add(new
                                {
                                    id = reader.GetInt32(0),
                                    name = reader.IsDBNull(1) ? "" : reader.GetString(1),
                                    arabicName = reader.IsDBNull(2) ? "" : reader.GetString(2)
                                });
                            }
                        }
                    }
                }

                return Ok(families);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching families");
                return StatusCode(500, new { message = "Error searching families", error = ex.Message });
            }
        }
    }
}




















