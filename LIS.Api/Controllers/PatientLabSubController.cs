using LIS.Api.Data;
using LIS.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LIS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PatientLabSubController : ControllerBase
    {
        private readonly LISDbContext _context;
        private readonly ILogger<PatientLabSubController> _logger;

        public PatientLabSubController(LISDbContext context, ILogger<PatientLabSubController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all patient lab sub-tests for a specific patient lab test ID
        /// </summary>
        [HttpGet("byPatientLabTest/{patientLabTestId:int}")]
        public async Task<ActionResult<IEnumerable<PatientLabSub>>> GetByPatientLabTestId(int patientLabTestId)
        {
            try
            {
                var subTests = await _context.PatientLabSubs
                    .Where(s => s.PatientLabTestID == patientLabTestId && s.IsDeleted == false)
                    .OrderBy(s => s.DisplayOrder)
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} sub-tests for patient lab test ID {PatientLabTestId}", 
                    subTests.Count, patientLabTestId);

                return Ok(subTests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving sub-tests for patient lab test ID {PatientLabTestId}", patientLabTestId);
                return StatusCode(500, "An error occurred while retrieving sub-tests");
            }
        }

        /// <summary>
        /// Get a specific patient lab sub-test by ID
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<PatientLabSub>> GetById(int id)
        {
            try
            {
                var subTest = await _context.PatientLabSubs
                    .FirstOrDefaultAsync(s => s.ID == id && s.IsDeleted == false);

                if (subTest == null)
                {
                    _logger.LogWarning("Patient lab sub-test with ID {Id} not found", id);
                    return NotFound(new { message = $"Patient lab sub-test with ID {id} not found" });
                }

                return Ok(subTest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving patient lab sub-test {Id}", id);
                return StatusCode(500, "An error occurred while retrieving the patient lab sub-test");
            }
        }

        /// <summary>
        /// Batch update multiple patient lab sub-tests
        /// </summary>
        [HttpPut("batch")]
        public async Task<ActionResult> BatchUpdate([FromBody] List<PatientLabSub> subTests)
        {
            try
            {
                // Use raw SQL to avoid trigger issues with EF Core's OUTPUT clause
                var updateCount = 0;
                
                using var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();
                
                foreach (var input in subTests)
                {
                    using var command = connection.CreateCommand();
                    command.CommandText = @"
                        UPDATE PatientLabSub 
                        SET Min = @min, 
                            Max = @max, 
                            Result = @result, 
                            ModifiedDate = @modifiedDate
                        WHERE ID = @id AND IsDeleted = 0";
                    
                    var minParam = command.CreateParameter();
                    minParam.ParameterName = "@min";
                    minParam.Value = input.Min ?? (object)DBNull.Value;
                    command.Parameters.Add(minParam);
                    
                    var maxParam = command.CreateParameter();
                    maxParam.ParameterName = "@max";
                    maxParam.Value = input.Max ?? (object)DBNull.Value;
                    command.Parameters.Add(maxParam);
                    
                    var resultParam = command.CreateParameter();
                    resultParam.ParameterName = "@result";
                    resultParam.Value = input.Result ?? (object)DBNull.Value;
                    command.Parameters.Add(resultParam);
                    
                    var modifiedDateParam = command.CreateParameter();
                    modifiedDateParam.ParameterName = "@modifiedDate";
                    modifiedDateParam.Value = DateTime.Now;
                    command.Parameters.Add(modifiedDateParam);
                    
                    var idParam = command.CreateParameter();
                    idParam.ParameterName = "@id";
                    idParam.Value = input.ID;
                    command.Parameters.Add(idParam);
                    
                    var rowsAffected = await command.ExecuteNonQueryAsync();
                    updateCount += rowsAffected;
                }

                _logger.LogInformation("Batch updated {Count} patient lab sub-tests", updateCount);
                return Ok(new { message = "Sub-tests updated successfully", count = updateCount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error batch updating patient lab sub-tests");
                return StatusCode(500, new { message = "An error occurred while updating the sub-tests", error = ex.Message });
            }
        }
    }
}

