using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using LIS.Api.Models;
using System.Data;

namespace LIS.Api.Controllers
{
    [ApiController]
    [Route("api/HospitalDefinition/[controller]")]
    public class InsuranceController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<InsuranceController> _logger;

        public InsuranceController(IConfiguration configuration, ILogger<InsuranceController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<InsuranceOption>>> GetInsurances()
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("HospitalDefinitionConnection");
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    return StatusCode(500, new { message = "HospitalDefinitionConnection string is not configured" });
                }

                var insurances = new List<InsuranceOption>();

                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var sql = @"
                        SELECT [ID], [Description], [ArabicDescription], [Code]
                        FROM HospitalDefinition.dbo.Insurance
                        WHERE [IsDeleted] = 0 OR [IsDeleted] IS NULL
                        ORDER BY [Description]";

                    using (var command = new SqlCommand(sql, connection))
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            insurances.Add(new InsuranceOption
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("ID")),
                                Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? string.Empty : reader.GetString(reader.GetOrdinal("Description")),
                                ArabicDescription = reader.IsDBNull(reader.GetOrdinal("ArabicDescription")) ? null : reader.GetString(reader.GetOrdinal("ArabicDescription")),
                                Code = reader.IsDBNull(reader.GetOrdinal("Code")) ? null : reader.GetString(reader.GetOrdinal("Code"))
                            });
                        }
                    }
                }

                return Ok(insurances);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving insurance options");
                return StatusCode(500, new { message = "An error occurred while retrieving insurance options", error = ex.Message });
            }
        }
    }
}
