using LIS.Api.Data;
using LIS.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LIS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LabTestSubController : ControllerBase
    {
        private readonly LISDbContext _context;
        private readonly ILogger<LabTestSubController> _logger;

        public LabTestSubController(LISDbContext context, ILogger<LabTestSubController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all lab test sub-tests
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<LabTestSub>>> GetAll()
        {
            try
            {
                var items = await _context.LabTestSubs
                    .Where(x => !x.IsDeleted)
                    .OrderBy(x => x.LabTest)
                    .ThenBy(x => x.DisplayOrder)
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} lab test sub-tests from database", items.Count);
                return Ok(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving lab test sub-tests from database");
                return StatusCode(500, "An error occurred while retrieving lab test sub-tests");
            }
        }

        /// <summary>
        /// Get lab test sub-tests by LabTestID
        /// </summary>
        [HttpGet("byLabTest/{labTestId:int}")]
        public async Task<ActionResult<IEnumerable<LabTestSub>>> GetByLabTestId(int labTestId)
        {
            try
            {
                var items = await _context.LabTestSubs
                    .Where(x => x.LabTest == labTestId && !x.IsDeleted)
                    .OrderBy(x => x.DisplayOrder)
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} sub-tests for lab test {LabTestId}", items.Count, labTestId);
                return Ok(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving sub-tests for lab test {LabTestId}", labTestId);
                return StatusCode(500, "An error occurred while retrieving sub-tests");
            }
        }

        /// <summary>
        /// Get a specific lab test sub-test by ID
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<LabTestSub>> GetById(int id)
        {
            try
            {
                var item = await _context.LabTestSubs.FindAsync(id);
                if (item == null || item.IsDeleted)
                {
                    return NotFound(new { message = $"Lab test sub-test with ID {id} not found" });
                }
                return Ok(item);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving lab test sub-test {Id}", id);
                return StatusCode(500, "An error occurred while retrieving the lab test sub-test");
            }
        }

        /// <summary>
        /// Create a new lab test sub-test
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<LabTestSub>> Create([FromBody] LabTestSub input)
        {
            try
            {
                input.ID = 0;
                input.CreatedDate = DateTime.UtcNow;
                input.IsDeleted = false;

                _context.LabTestSubs.Add(input);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created lab test sub-test with ID {Id}", input.ID);
                return CreatedAtAction(nameof(GetById), new { id = input.ID }, input);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating lab test sub-test");
                return StatusCode(500, "An error occurred while creating the lab test sub-test");
            }
        }

        /// <summary>
        /// Update an existing lab test sub-test
        /// </summary>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] LabTestSub input)
        {
            try
            {
                if (id != input.ID)
                {
                    return BadRequest(new { message = "ID mismatch" });
                }

                var existing = await _context.LabTestSubs.FindAsync(id);
                if (existing == null || existing.IsDeleted)
                {
                    return NotFound(new { message = $"Lab test sub-test with ID {id} not found" });
                }

                input.ModifiedDate = DateTime.UtcNow;
                _context.Entry(existing).CurrentValues.SetValues(input);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated lab test sub-test with ID {Id}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating lab test sub-test {Id}", id);
                return StatusCode(500, "An error occurred while updating the lab test sub-test");
            }
        }

        /// <summary>
        /// Soft delete a lab test sub-test
        /// </summary>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var item = await _context.LabTestSubs.FindAsync(id);
                if (item == null || item.IsDeleted)
                {
                    return NotFound(new { message = $"Lab test sub-test with ID {id} not found" });
                }

                item.IsDeleted = true;
                item.ModifiedDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Soft deleted lab test sub-test with ID {Id}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting lab test sub-test {Id}", id);
                return StatusCode(500, "An error occurred while deleting the lab test sub-test");
            }
        }
    }
}

