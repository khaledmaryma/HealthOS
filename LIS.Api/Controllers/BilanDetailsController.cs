using LIS.Api.Data;
using LIS.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LIS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BilanDetailsController : ControllerBase
    {
        private readonly LISDbContext _context;
        private readonly ILogger<BilanDetailsController> _logger;

        public BilanDetailsController(LISDbContext context, ILogger<BilanDetailsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all bilan details from the EMR database.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BilanDetail>>> GetAll()
        {
            try
            {
                var details = await _context.BilanDetails
                    .FromSqlRaw("SELECT * FROM EMR.dbo.BilanDetail WHERE IsDeleted = 0 OR IsDeleted IS NULL")
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} bilan details from EMR database", details.Count);
                return Ok(details);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving bilan details from EMR database");
                return StatusCode(500, "An error occurred while retrieving bilan details");
            }
        }

        /// <summary>
        /// Get a specific bilan detail by ID.
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<BilanDetail>> GetById(int id)
        {
            try
            {
                var detail = await _context.BilanDetails
                    .FromSqlRaw("SELECT * FROM EMR.dbo.BilanDetail WHERE ID = {0} AND (IsDeleted = 0 OR IsDeleted IS NULL)", id)
                    .FirstOrDefaultAsync();

                if (detail == null)
                {
                    _logger.LogWarning("BilanDetail with ID {Id} not found in EMR database", id);
                    return NotFound(new { message = $"BilanDetail with ID {id} not found" });
                }

                return Ok(detail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving bilan detail {Id} from EMR database", id);
                return StatusCode(500, "An error occurred while retrieving the bilan detail");
            }
        }

        /// <summary>
        /// Get bilan details by BilanID.
        /// </summary>
        [HttpGet("by-bilan/{bilanId:int}")]
        public async Task<ActionResult<IEnumerable<BilanDetail>>> GetByBilanId(int bilanId)
        {
            try
            {
                var details = await _context.BilanDetails
                    .FromSqlRaw(@"SELECT * FROM EMR.dbo.BilanDetail 
                                  WHERE BilanID = {0} AND (IsDeleted = 0 OR IsDeleted IS NULL)", bilanId)
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} bilan details for BilanID {BilanId}", details.Count, bilanId);
                return Ok(details);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving bilan details for BilanID {BilanId} from EMR database", bilanId);
                return StatusCode(500, "An error occurred while retrieving bilan details");
            }
        }
    }
}
