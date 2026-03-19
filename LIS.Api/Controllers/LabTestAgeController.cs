using LIS.Api.Data;
using LIS.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LIS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LabTestAgeController : ControllerBase
    {
        private readonly LISDbContext _context;
        private readonly ILogger<LabTestAgeController> _logger;

        public LabTestAgeController(LISDbContext context, ILogger<LabTestAgeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all lab test age ranges
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<LabTestAge>>> GetAll()
        {
            try
            {
                var items = await _context.LabTestAges
                    .Where(x => !x.IsDeleted)
                    .OrderBy(x => x.LabTest)
                    .ThenBy(x => x.Lower)
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} lab test age ranges from database", items.Count);
                return Ok(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving lab test age ranges from database");
                return StatusCode(500, "An error occurred while retrieving lab test age ranges");
            }
        }

        /// <summary>
        /// Get lab test age ranges by LabTestID
        /// </summary>
        [HttpGet("byLabTest/{labTestId:int}")]
        public async Task<ActionResult<IEnumerable<LabTestAge>>> GetByLabTestId(int labTestId)
        {
            try
            {
                var items = await _context.LabTestAges
                    .Where(x => x.LabTest == labTestId && !x.IsDeleted)
                    .OrderBy(x => x.Lower)
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} age ranges for lab test {LabTestId}", items.Count, labTestId);
                return Ok(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving age ranges for lab test {LabTestId}", labTestId);
                return StatusCode(500, "An error occurred while retrieving age ranges");
            }
        }

        /// <summary>
        /// Get a specific lab test age range by ID
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<LabTestAge>> GetById(int id)
        {
            try
            {
                var item = await _context.LabTestAges.FindAsync(id);
                if (item == null || item.IsDeleted)
                {
                    return NotFound(new { message = $"Lab test age range with ID {id} not found" });
                }
                return Ok(item);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving lab test age range {Id}", id);
                return StatusCode(500, "An error occurred while retrieving the lab test age range");
            }
        }

        /// <summary>
        /// Create a new lab test age range
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<LabTestAge>> Create([FromBody] LabTestAge input)
        {
            try
            {
                input.ID = 0;
                input.CreatedDate = DateTime.UtcNow;
                input.IsDeleted = false;

                _context.LabTestAges.Add(input);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created lab test age range with ID {Id}", input.ID);
                return CreatedAtAction(nameof(GetById), new { id = input.ID }, input);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating lab test age range");
                return StatusCode(500, "An error occurred while creating the lab test age range");
            }
        }

        /// <summary>
        /// Update an existing lab test age range
        /// </summary>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] LabTestAge input)
        {
            try
            {
                if (id != input.ID)
                {
                    return BadRequest(new { message = "ID mismatch" });
                }

                var existing = await _context.LabTestAges.FindAsync(id);
                if (existing == null || existing.IsDeleted)
                {
                    return NotFound(new { message = $"Lab test age range with ID {id} not found" });
                }

                input.ModifiedDate = DateTime.UtcNow;
                _context.Entry(existing).CurrentValues.SetValues(input);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated lab test age range with ID {Id}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating lab test age range {Id}", id);
                return StatusCode(500, "An error occurred while updating the lab test age range");
            }
        }

        /// <summary>
        /// Soft delete a lab test age range
        /// </summary>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var item = await _context.LabTestAges.FindAsync(id);
                if (item == null || item.IsDeleted)
                {
                    return NotFound(new { message = $"Lab test age range with ID {id} not found" });
                }

                item.IsDeleted = true;
                item.ModifiedDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Soft deleted lab test age range with ID {Id}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting lab test age range {Id}", id);
                return StatusCode(500, "An error occurred while deleting the lab test age range");
            }
        }
    }
}

