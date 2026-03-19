using LIS.Api.Data;
using LIS.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LIS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BacteriaController : ControllerBase
    {
        private readonly LISDbContext _context;
        private readonly ILogger<BacteriaController> _logger;

        public BacteriaController(LISDbContext context, ILogger<BacteriaController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all bacteria
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetAll([FromQuery] int? germId = null)
        {
            try
            {
                var query = _context.Bacterias.Where(b => !b.IsDeleted);

                // Filter by germ ID if provided
                if (germId.HasValue)
                {
                    query = query.Where(b => b.GermId == germId.Value);
                }

                var bacteria = await query
                    .Select(b => new
                    {
                        id = b.Id,
                        germId = b.GermId,
                        description = b.Description ?? "",
                        isPanic = b.IsPanic
                    })
                    .ToListAsync();

                var filterMsg = germId.HasValue ? $" (filtered by germ ID: {germId.Value})" : "";
                _logger.LogInformation("Retrieved {Count} bacteria from database{Filter}", bacteria.Count, filterMsg);
                return Ok(bacteria);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving bacteria");
                return StatusCode(500, "An error occurred while retrieving bacteria");
            }
        }

        /// <summary>
        /// Search bacteria by description
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<object>>> Search([FromQuery] string query, [FromQuery] int? germId = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return BadRequest("Search query cannot be empty");
                }

                var bacteriaQuery = _context.Bacterias
                    .Where(b => !b.IsDeleted &&
                           b.Description!.Contains(query));

                // Filter by germ ID if provided
                if (germId.HasValue)
                {
                    bacteriaQuery = bacteriaQuery.Where(b => b.GermId == germId.Value);
                }

                var bacteria = await bacteriaQuery
                    .Select(b => new
                    {
                        id = b.Id,
                        germId = b.GermId,
                        description = b.Description ?? "",
                        isPanic = b.IsPanic
                    })
                    .Take(50)
                    .ToListAsync();

                _logger.LogInformation("Search for '{Query}' returned {Count} bacteria", query, bacteria.Count);
                return Ok(bacteria);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching bacteria with query '{Query}'", query);
                return StatusCode(500, "An error occurred while searching bacteria");
            }
        }

        /// <summary>
        /// Get antibiotics for a specific germ (via GermAntibiotic relationship)
        /// </summary>
        [HttpGet("antibiotics/{germId:int}")]
        public async Task<ActionResult<IEnumerable<object>>> GetAntibioticsForGerm(int germId)
        {
            try
            {
                var antibiotics = await (from ga in _context.GermAntibiotics
                                        join a in _context.Antibiotics on ga.AntibioticId equals a.Id
                                        where ga.GermId == germId && ga.IsDeleted == false && a.IsDeleted == false
                                        select new
                                        {
                                            id = ga.AntibioticId,
                                            description = a.Description
                                        }).ToListAsync();

                _logger.LogInformation("Retrieved {Count} antibiotics for germ ID {GermId}", antibiotics.Count, germId);
                return Ok(antibiotics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving antibiotics for germ ID {GermId}", germId);
                return StatusCode(500, "An error occurred while retrieving antibiotics");
            }
        }
    }
}

