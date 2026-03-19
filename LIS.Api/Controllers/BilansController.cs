using LIS.Api.Data;
using LIS.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LIS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BilansController : ControllerBase
    {
        private readonly LISDbContext _context;
        private readonly ILogger<BilansController> _logger;

        public BilansController(LISDbContext context, ILogger<BilansController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all bilans from the EMR database.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Bilan>>> GetAll()
        {
            try
            {
                var bilans = await _context.Bilans
                    .FromSqlRaw("SELECT * FROM EMR.dbo.Bilan WHERE IsDeleted = 0 OR IsDeleted IS NULL")
                    .OrderBy(b => b.Description)
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} bilans from EMR database", bilans.Count);
                return Ok(bilans);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving bilans from EMR database");
                return StatusCode(500, "An error occurred while retrieving bilans");
            }
        }

        /// <summary>
        /// Get a specific bilan by ID.
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Bilan>> GetById(int id)
        {
            try
            {
                var bilan = await _context.Bilans
                    .FromSqlRaw("SELECT * FROM EMR.dbo.Bilan WHERE ID = {0} AND (IsDeleted = 0 OR IsDeleted IS NULL)", id)
                    .FirstOrDefaultAsync();

                if (bilan == null)
                {
                    _logger.LogWarning("Bilan with ID {Id} not found in EMR database", id);
                    return NotFound(new { message = $"Bilan with ID {id} not found" });
                }

                return Ok(bilan);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving bilan {Id} from EMR database", id);
                return StatusCode(500, "An error occurred while retrieving the bilan");
            }
        }

        /// <summary>
        /// Get bilan details for a specific bilan.
        /// </summary>
        [HttpGet("{id:int}/details")]
        public async Task<ActionResult<IEnumerable<BilanDetail>>> GetDetails(int id)
        {
            try
            {
                var details = await _context.BilanDetails
                    .FromSqlRaw(@"SELECT * FROM EMR.dbo.BilanDetail 
                                  WHERE BilanID = {0} AND (IsDeleted = 0 OR IsDeleted IS NULL)", id)
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} bilan details for BilanID {BilanId}", details.Count, id);
                return Ok(details);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving bilan details for BilanID {BilanId} from EMR database", id);
                return StatusCode(500, "An error occurred while retrieving bilan details");
            }
        }
    }
}
