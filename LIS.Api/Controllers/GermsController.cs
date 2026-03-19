using LIS.Api.Data;
using LIS.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LIS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GermsController : ControllerBase
    {
        private readonly LISDbContext _context;
        private readonly ILogger<GermsController> _logger;

        public GermsController(LISDbContext context, ILogger<GermsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all germs
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Germs>>> GetGerms()
        {
            try
            {
                var germs = await _context.Germs
                    .Where(g => !g.IsDeleted)
                    .OrderBy(g => g.DisplayOrder)
                    .ThenBy(g => g.Description)
                    .ToListAsync();

                return Ok(germs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving germs");
                return StatusCode(500, "An error occurred while retrieving germs");
            }
        }

        /// <summary>
        /// Get a specific germ by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<Germs>> GetGerm(int id)
        {
            try
            {
                var germ = await _context.Germs
                    .FirstOrDefaultAsync(g => g.Id == id && !g.IsDeleted);

                if (germ == null)
                {
                    return NotFound();
                }

                return Ok(germ);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving germ with ID {Id}", id);
                return StatusCode(500, "An error occurred while retrieving the germ");
            }
        }

        /// <summary>
        /// Create a new germ
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<Germs>> CreateGerm([FromBody] CreateGermRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest("Germ data is required");
                }

                // Validate required fields
                if (string.IsNullOrWhiteSpace(request.Code))
                {
                    return BadRequest("Code is required");
                }

                if (string.IsNullOrWhiteSpace(request.Description))
                {
                    return BadRequest("Description is required");
                }

                // Check if code already exists
                var existingGerm = await _context.Germs
                    .FirstOrDefaultAsync(g => g.Code == request.Code && !g.IsDeleted);

                if (existingGerm != null)
                {
                    return BadRequest("A germ with this code already exists");
                }

                var germ = new Germs
                {
                    Code = request.Code.Trim(),
                    Description = request.Description.Trim(),
                    Identifier = request.Identifier?.Trim(),
                    DisplayOrder = request.DisplayOrder?.Trim(),
                    CreatedBy = request.CreatedBy,
                    CreatedDate = DateTime.Now,
                    IsDeleted = false
                };

                _context.Germs.Add(germ);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created germ with ID {Id} and code {Code}", germ.Id, germ.Code);

                return CreatedAtAction(nameof(GetGerm), new { id = germ.Id }, germ);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating germ");
                return StatusCode(500, "An error occurred while creating the germ");
            }
        }

        /// <summary>
        /// Update an existing germ
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateGerm(int id, [FromBody] UpdateGermRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest("Germ data is required");
                }

                var germ = await _context.Germs
                    .FirstOrDefaultAsync(g => g.Id == id && !g.IsDeleted);

                if (germ == null)
                {
                    return NotFound();
                }

                // Validate required fields
                if (string.IsNullOrWhiteSpace(request.Code))
                {
                    return BadRequest("Code is required");
                }

                if (string.IsNullOrWhiteSpace(request.Description))
                {
                    return BadRequest("Description is required");
                }

                // Check if code already exists (excluding current record)
                var existingGerm = await _context.Germs
                    .FirstOrDefaultAsync(g => g.Code == request.Code && g.Id != id && !g.IsDeleted);

                if (existingGerm != null)
                {
                    return BadRequest("A germ with this code already exists");
                }

                // Update properties
                germ.Code = request.Code.Trim();
                germ.Description = request.Description.Trim();
                germ.Identifier = request.Identifier?.Trim();
                germ.DisplayOrder = request.DisplayOrder?.Trim();
                germ.ModifiedBy = request.ModifiedBy;
                germ.ModifiedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated germ with ID {Id}", id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating germ with ID {Id}", id);
                return StatusCode(500, "An error occurred while updating the germ");
            }
        }

        /// <summary>
        /// Delete a germ (soft delete)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGerm(int id)
        {
            try
            {
                var germ = await _context.Germs
                    .FirstOrDefaultAsync(g => g.Id == id && !g.IsDeleted);

                if (germ == null)
                {
                    return NotFound();
                }

                // Soft delete
                germ.IsDeleted = true;
                germ.ModifiedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted germ with ID {Id}", id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting germ with ID {Id}", id);
                return StatusCode(500, "An error occurred while deleting the germ");
            }
        }
    }

    public class CreateGermRequest
    {
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Identifier { get; set; }
        public string? DisplayOrder { get; set; }
        public int? CreatedBy { get; set; }
    }

    public class UpdateGermRequest
    {
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Identifier { get; set; }
        public string? DisplayOrder { get; set; }
        public int? ModifiedBy { get; set; }
    }
}