using LIS.Api.Data;
using LIS.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LIS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LabTestGynecoController : ControllerBase
    {
        private readonly LISDbContext _context;
        private readonly ILogger<LabTestGynecoController> _logger;

        public LabTestGynecoController(LISDbContext context, ILogger<LabTestGynecoController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all lab test gyneco references
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<LabTestGyneco>>> GetAll()
        {
            try
            {
                var items = await _context.LabTestGynecos
                    .Where(x => !x.IsDeleted)
                    .OrderBy(x => x.LabTest)
                    .ThenBy(x => x.Description)
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} lab test gyneco references from database", items.Count);
                return Ok(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving lab test gyneco references from database");
                return StatusCode(500, "An error occurred while retrieving lab test gyneco references");
            }
        }

        /// <summary>
        /// Get lab test gyneco references by LabTestID
        /// </summary>
        [HttpGet("byLabTest/{labTestId:int}")]
        public async Task<ActionResult<IEnumerable<LabTestGyneco>>> GetByLabTestId(int labTestId)
        {
            try
            {
                var items = await _context.LabTestGynecos
                    .Where(x => x.LabTest == labTestId && !x.IsDeleted)
                    .OrderBy(x => x.Description)
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} gyneco references for lab test {LabTestId}", items.Count, labTestId);
                return Ok(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving gyneco references for lab test {LabTestId}", labTestId);
                return StatusCode(500, "An error occurred while retrieving gyneco references");
            }
        }

        /// <summary>
        /// Get a specific lab test gyneco reference by ID
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<LabTestGyneco>> GetById(int id)
        {
            try
            {
                var item = await _context.LabTestGynecos.FindAsync(id);
                if (item == null || item.IsDeleted)
                {
                    return NotFound(new { message = $"Lab test gyneco reference with ID {id} not found" });
                }
                return Ok(item);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving lab test gyneco reference {Id}", id);
                return StatusCode(500, "An error occurred while retrieving the lab test gyneco reference");
            }
        }

        /// <summary>
        /// Create a new lab test gyneco reference
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<LabTestGyneco>> Create([FromBody] LabTestGyneco input)
        {
            try
            {
                input.ID = 0;
                input.CreatedDate = DateTime.UtcNow;
                input.IsDeleted = false;

                _context.LabTestGynecos.Add(input);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created lab test gyneco reference with ID {Id}", input.ID);
                return CreatedAtAction(nameof(GetById), new { id = input.ID }, input);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating lab test gyneco reference");
                return StatusCode(500, "An error occurred while creating the lab test gyneco reference");
            }
        }

        /// <summary>
        /// Update an existing lab test gyneco reference
        /// </summary>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] LabTestGyneco input)
        {
            try
            {
                if (id != input.ID)
                {
                    return BadRequest(new { message = "ID mismatch" });
                }

                var existing = await _context.LabTestGynecos.FindAsync(id);
                if (existing == null || existing.IsDeleted)
                {
                    return NotFound(new { message = $"Lab test gyneco reference with ID {id} not found" });
                }

                input.ModifiedDate = DateTime.UtcNow;
                _context.Entry(existing).CurrentValues.SetValues(input);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated lab test gyneco reference with ID {Id}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating lab test gyneco reference {Id}", id);
                return StatusCode(500, "An error occurred while updating the lab test gyneco reference");
            }
        }

        /// <summary>
        /// Soft delete a lab test gyneco reference
        /// </summary>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var item = await _context.LabTestGynecos.FindAsync(id);
                if (item == null || item.IsDeleted)
                {
                    return NotFound(new { message = $"Lab test gyneco reference with ID {id} not found" });
                }

                item.IsDeleted = true;
                item.ModifiedDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Soft deleted lab test gyneco reference with ID {Id}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting lab test gyneco reference {Id}", id);
                return StatusCode(500, "An error occurred while deleting the lab test gyneco reference");
            }
        }
    }
}

