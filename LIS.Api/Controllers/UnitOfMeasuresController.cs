using LIS.Api.Data;
using LIS.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LIS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UnitOfMeasuresController : ControllerBase
    {
        private readonly LISDbContext _context;
        private readonly ILogger<UnitOfMeasuresController> _logger;

        public UnitOfMeasuresController(LISDbContext context, ILogger<UnitOfMeasuresController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all units of measure from the EMR database
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UnitOfMeasure>>> GetAll()
        {
            try
            {
                // Query EMR database using cross-database query
                var unitOfMeasures = await _context.UnitOfMeasures
                    .FromSqlRaw("SELECT * FROM EMR.dbo.UnitOfMeasure WHERE IsDeleted = 0 OR IsDeleted IS NULL")
                    .OrderBy(uom => uom.Description)
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} units of measure from EMR database", unitOfMeasures.Count);
                return Ok(unitOfMeasures);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving units of measure from EMR database");
                return StatusCode(500, "An error occurred while retrieving units of measure");
            }
        }

        /// <summary>
        /// Get a specific unit of measure by ID
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<UnitOfMeasure>> GetById(int id)
        {
            try
            {
                var unitOfMeasure = await _context.UnitOfMeasures
                    .FromSqlRaw("SELECT * FROM EMR.dbo.UnitOfMeasure WHERE ID = {0} AND (IsDeleted = 0 OR IsDeleted IS NULL)", id)
                    .FirstOrDefaultAsync();

                if (unitOfMeasure == null)
                {
                    _logger.LogWarning("Unit of measure with ID {Id} not found in EMR database", id);
                    return NotFound(new { message = $"Unit of measure with ID {id} not found" });
                }
                return Ok(unitOfMeasure);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving unit of measure {Id} from EMR database", id);
                return StatusCode(500, "An error occurred while retrieving the unit of measure");
            }
        }
    }
}

