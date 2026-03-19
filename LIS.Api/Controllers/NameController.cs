using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace LIS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NameController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<NameController> _logger;

        public NameController(IConfiguration configuration, ILogger<NameController> logger)
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
                
                var names = new List<object>();

                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var whereClause = "IsDeleted = 0";
                    if (!string.IsNullOrWhiteSpace(query))
                    {
                        whereClause += " AND Name LIKE @query";
                    }

                    var sql = $@"
                        SELECT TOP 50 ID, Name, NameA as ArabicName, Gender
                        FROM Name WITH (NOLOCK)
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
                                names.Add(new
                                {
                                    id = reader.GetInt32(0),
                                    name = reader.IsDBNull(1) ? "" : reader.GetString(1),
                                    arabicName = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                    gender = reader.IsDBNull(3) ? null : reader.GetString(3)
                                });
                            }
                        }
                    }
                }

                return Ok(names);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching names");
                return StatusCode(500, new { message = "Error searching names", error = ex.Message });
            }
        }
    }
}

