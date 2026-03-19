using LIS.Api.Data;
using LIS.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LIS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ResultTypesController : ControllerBase
    {
        private readonly LISDbContext _context;
        private readonly ILogger<ResultTypesController> _logger;

        public ResultTypesController(LISDbContext context, ILogger<ResultTypesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all result types from the database
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ResultType>>> GetAll()
        {
            try
            {
                var resultTypes = await _context.ResultTypes
                    .OrderBy(rt => rt.ID)
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} result types from database", resultTypes.Count);
                return Ok(resultTypes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving result types from database");
                return StatusCode(500, "An error occurred while retrieving result types");
            }
        }

        /// <summary>
        /// Get a specific result type by ID
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ResultType>> GetById(int id)
        {
            try
            {
                var resultType = await _context.ResultTypes
                    .FirstOrDefaultAsync(rt => rt.ID == id);
                if (resultType == null)
                {
                    _logger.LogWarning("Result type with ID {Id} not found", id);
                    return NotFound(new { message = $"Result type with ID {id} not found" });
                }
                return Ok(resultType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving result type {Id} from database", id);
                return StatusCode(500, "An error occurred while retrieving the result type");
            }
        }
    }
}

