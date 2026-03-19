using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LIS.Api.Data;
using LIS.Api.Models;

namespace LIS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HospitalConfigurationController : ControllerBase
    {
        private readonly ConfigurationDbContext _context;
        private readonly ILogger<HospitalConfigurationController> _logger;

        public HospitalConfigurationController(ConfigurationDbContext context, ILogger<HospitalConfigurationController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // DTO for frontend (with logo as base64)
        public class HospitalConfigDto
        {
            public int Id { get; set; }
            public string? HospitalName { get; set; }
            public string? HospitalNameArabic { get; set; }
            public string? HospitalAddress { get; set; }
            public string? HospitalAddressArabic { get; set; }
            public string? HospitalPhone { get; set; }
            public string? HospitalFax { get; set; }
            public string? LogoBase64 { get; set; } // Logo as base64 string
        }

        // GET: api/HospitalConfiguration
        [HttpGet]
        public async Task<ActionResult<HospitalConfigDto>> GetHospitalConfiguration()
        {
            try
            {
                // Query from Configuration database (cross-database query)
                // Get first record (usually there's only one)
                var config = await _context.HospitalConfigurations
                    .AsNoTracking()
                    .FirstOrDefaultAsync();

                if (config == null)
                {
                    _logger.LogWarning("No hospital configuration found");
                    return NotFound(new { message = "No hospital configuration found" });
                }

                // Convert to DTO with logo as base64
                var dto = new HospitalConfigDto
                {
                    Id = config.ID,
                    HospitalName = config.HospitalName,
                    HospitalNameArabic = config.HospitalNameArabic,
                    HospitalAddress = config.HospitalAddress,
                    HospitalAddressArabic = config.HospitalAddressArabic,
                    HospitalPhone = config.HospitalPhone,
                    HospitalFax = config.HospitalFax,
                    LogoBase64 = config.HospitalLogo != null && config.HospitalLogo.Length > 0
                        ? Convert.ToBase64String(config.HospitalLogo)
                        : null
                };

                _logger.LogInformation("Retrieved hospital configuration: {HospitalName}", config.HospitalName);
                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving hospital configuration");
                return StatusCode(500, new { message = "Error retrieving hospital configuration", error = ex.Message });
            }
        }

    }
}

