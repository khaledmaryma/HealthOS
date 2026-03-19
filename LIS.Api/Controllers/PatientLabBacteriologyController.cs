using LIS.Api.Data;
using LIS.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LIS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PatientLabBacteriologyController : ControllerBase
    {
        private readonly LISDbContext _context;
        private readonly ILogger<PatientLabBacteriologyController> _logger;

        public PatientLabBacteriologyController(LISDbContext context, ILogger<PatientLabBacteriologyController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get bacteriology header and details for a specific patient lab result ID (ResultType=3 Antibiogram)
        /// </summary>
        [HttpGet("byPatientLabResult/{patientLabResultId:int}")]
        public async Task<ActionResult> GetByPatientLabResultId(int patientLabResultId)
        {
            try
            {
                var header = await _context.PatientLabBacteriologyHeaders
                    .FirstOrDefaultAsync(h => h.PatientLabResultId == patientLabResultId && h.IsDeleted == false);

                if (header == null)
                {
                    return NotFound(new { message = "No bacteriology header found for this lab result" });
                }

                var details = await _context.PatientLabBacteriologies
                    .Where(b => b.PatientHeader == header.Id && b.IsDeleted == false)
                    .ToListAsync();

                return Ok(new { header, details });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving bacteriology data for patient lab result ID {PatientLabResultId}", patientLabResultId);
                return StatusCode(500, "An error occurred while retrieving bacteriology data");
            }
        }

        /// <summary>
        /// Create or update bacteriology header and create detail records for selected germ
        /// </summary>
        [HttpPost("createForGerm")]
        public async Task<ActionResult> CreateForGerm([FromBody] CreateBacteriologyRequest request)
        {
            try
            {
                if (request.PatientLabResultId <= 0 || request.GermId <= 0)
                {
                    return BadRequest(new { message = "PatientLabResultId and GermId are required" });
                }

                // Check if the germ exists
                var germ = await _context.Germs
                    .FirstOrDefaultAsync(g => g.Id == request.GermId && g.IsDeleted == false);

                if (germ == null)
                {
                    return NotFound(new { message = $"Germ with ID {request.GermId} not found" });
                }

                // Check if the patient lab result exists (should be ResultType = 3 for Antibiogram)
                var patientLabResult = await _context.PatientLabResults
                    .FirstOrDefaultAsync(r => r.ID == request.PatientLabResultId && r.IsDeleted == false);

                if (patientLabResult == null)
                {
                    return NotFound(new { message = $"Patient lab result with ID {request.PatientLabResultId} not found" });
                }

                // Check if a bacteriology header already exists for this patient lab result
                var bacteriologyHeader = await _context.PatientLabBacteriologyHeaders
                    .FirstOrDefaultAsync(h => h.PatientLabResultId == request.PatientLabResultId && h.IsDeleted == false);

                // If no header exists, create one
                if (bacteriologyHeader == null)
                {
                    bacteriologyHeader = new PatientLabBacteriologyHeader
                    {
                        PatientLabResultId = request.PatientLabResultId,
                        GermsId = request.GermId,
                        Germ = germ.Description,
                        BacterieId = request.BacteriaId,
                        Bacteria = request.BacteriaName,
                        DateTime = System.DateTime.Now,
                        IsDeleted = false,
                        CreatedBy = request.CreatedBy ?? 1,
                        CreatedDate = System.DateTime.Now,
                        Comments = request.Comments
                    };

                    _context.PatientLabBacteriologyHeaders.Add(bacteriologyHeader);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Created new bacteriology header with ID {HeaderId} for patient lab result {PatientLabResultId}, germ: {Germ}, bacteria: {Bacteria}",
                        bacteriologyHeader.Id, request.PatientLabResultId, germ.Description, request.BacteriaName);
                }
                else
                {
                    // Update header with current germ and bacteria info
                    bacteriologyHeader.GermsId = request.GermId;
                    bacteriologyHeader.Germ = germ.Description;
                    bacteriologyHeader.BacterieId = request.BacteriaId;
                    bacteriologyHeader.Bacteria = request.BacteriaName;
                    bacteriologyHeader.ModifiedBy = request.CreatedBy ?? 1;
                    bacteriologyHeader.ModifiedDate = System.DateTime.Now;
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Updated bacteriology header ID {HeaderId} with germ: {Germ}, bacteria: {Bacteria}",
                        bacteriologyHeader.Id, germ.Description, request.BacteriaName);
                }

                // Check if there are existing records for this header
                var existingRecords = await _context.PatientLabBacteriologies
                    .Where(b => b.PatientHeader == bacteriologyHeader.Id && b.IsDeleted == false)
                    .ToListAsync();

                // If existing records are for a different germ, soft-delete them
                if (existingRecords.Count > 0)
                {
                    var firstExistingCode = existingRecords.First().Code;
                    if (firstExistingCode != germ.Code)
                    {
                        _logger.LogInformation("Switching germ from {OldCode} to {NewCode} - soft deleting {Count} old records",
                            firstExistingCode, germ.Code, existingRecords.Count);
                        
                        foreach (var record in existingRecords)
                        {
                            record.IsDeleted = true;
                            record.ModifiedBy = request.CreatedBy ?? 1;
                            record.ModifiedDate = System.DateTime.Now;
                        }
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("Soft-deleted {Count} old bacteriology records", existingRecords.Count);
                    }
                    else
                    {
                        _logger.LogInformation("Records already exist for the same germ ({Code}), will skip duplicates", germ.Code);
                    }
                }

                // Get all antibiotics related to the selected germ
                var germAntibiotics = await (from ga in _context.GermAntibiotics
                                             join a in _context.Antibiotics on ga.AntibioticId equals a.Id
                                             where ga.GermId == request.GermId && ga.IsDeleted == false && a.IsDeleted == false
                                             select new
                                             {
                                                 ga.AntibioticId,
                                                 AntibioticDescription = a.Description
                                             }).ToListAsync();

                _logger.LogInformation("Found {Count} antibiotics for germ ID {GermId}", germAntibiotics.Count, request.GermId);
                
                if (germAntibiotics.Count == 0)
                {
                    _logger.LogWarning("No antibiotics found for germ ID {GermId}", request.GermId);
                    return Ok(new
                    {
                        message = "Bacteriology header created but no antibiotics found for this germ",
                        headerId = bacteriologyHeader.Id,
                        detailsCreated = 0
                    });
                }

                // Create PatientLabBacteriology records for each antibiotic
                var bacteriologyDetails = new List<PatientLabBacteriology>();
                int displayOrder = 1;

                foreach (var germAntibiotic in germAntibiotics)
                {
                    // Check if this combination already exists (only check if not switching germs)
                    var existingDetail = await _context.PatientLabBacteriologies
                        .FirstOrDefaultAsync(b =>
                            b.PatientHeader == bacteriologyHeader.Id &&
                            b.Code == germ.Code &&
                            b.AntibioticId == germAntibiotic.AntibioticId &&
                            b.IsDeleted == false);

                    if (existingDetail == null)
                    {
                        var bacteriologyDetail = new PatientLabBacteriology
                        {
                            PatientHeader = bacteriologyHeader.Id,
                            Code = germ.Code ?? string.Empty,
                            AntibioticId = germAntibiotic.AntibioticId,
                            AntibioticDescription = germAntibiotic.AntibioticDescription,
                            DateTime = System.DateTime.Now,
                            Resistant = false,
                            Intermediat = false,
                            Sensible = false,
                            Charge = string.Empty,
                            Diameter = string.Empty,
                            DisplayOrder = displayOrder.ToString(),
                            IsDeleted = false,
                            CreatedBy = request.CreatedBy ?? 1,
                            CreatedDate = System.DateTime.Now,
                            ModifiedBy = null,
                            ModifiedDate = null
                        };

                        bacteriologyDetails.Add(bacteriologyDetail);
                        _logger.LogInformation("Adding new record for antibiotic {AntibioticId} - {Description}", 
                            germAntibiotic.AntibioticId, germAntibiotic.AntibioticDescription);
                        displayOrder++;
                    }
                    else
                    {
                        _logger.LogInformation("Record already exists for antibiotic {AntibioticId}, skipping", 
                            germAntibiotic.AntibioticId);
                    }
                }

                if (bacteriologyDetails.Count > 0)
                {
                    _logger.LogWarning("⚠️ ABOUT TO INSERT {Count} NEW RECORDS INTO PatientLabBacteriology", bacteriologyDetails.Count);
                    _context.PatientLabBacteriologies.AddRange(bacteriologyDetails);
                    await _context.SaveChangesAsync();

                    _logger.LogWarning("✅ SUCCESSFULLY INSERTED {Count} bacteriology detail records for header {HeaderId}",
                        bacteriologyDetails.Count, bacteriologyHeader.Id);
                }
                else
                {
                    _logger.LogWarning("⚠️ NO NEW RECORDS TO INSERT - All records already exist");
                }

                // Always return all records (existing + newly created)
                var allRecords = await _context.PatientLabBacteriologies
                    .Where(b => b.PatientHeader == bacteriologyHeader.Id && b.IsDeleted == false)
                    .ToListAsync();

                _logger.LogInformation("Returning {Count} total bacteriology records", allRecords.Count);

                return Ok(new
                {
                    message = bacteriologyDetails.Count > 0 ? "Bacteriology records created successfully" : "Bacteriology records already exist",
                    headerId = bacteriologyHeader.Id,
                    detailsCreated = bacteriologyDetails.Count,
                    totalRecords = allRecords.Count,
                    details = allRecords
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating bacteriology records for patient lab result {PatientLabResultId} and germ {GermId}",
                    request.PatientLabResultId, request.GermId);
                return StatusCode(500, new { message = "An error occurred while creating bacteriology records", error = ex.Message });
            }
        }

        /// <summary>
        /// Batch update bacteriology detail records
        /// </summary>
        [HttpPut("batchUpdate")]
        public async Task<ActionResult> BatchUpdate([FromBody] List<PatientLabBacteriology> bacteriologyDetails)
        {
            try
            {
                var updateCount = 0;

                using var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();

                foreach (var detail in bacteriologyDetails)
                {
                    using var command = connection.CreateCommand();
                    command.CommandText = @"
                        UPDATE PatientLabBacteriology 
                        SET Resistant = @resistant,
                            Intermediat = @intermediat,
                            Sensible = @sensible,
                            Charge = @charge,
                            Diameter = @diameter,
                            ModifiedBy = @modifiedBy,
                            ModifiedDate = @modifiedDate
                        WHERE ID = @id AND IsDeleted = 0";

                    var resistantParam = command.CreateParameter();
                    resistantParam.ParameterName = "@resistant";
                    resistantParam.Value = detail.Resistant;
                    command.Parameters.Add(resistantParam);

                    var intermediatParam = command.CreateParameter();
                    intermediatParam.ParameterName = "@intermediat";
                    intermediatParam.Value = detail.Intermediat;
                    command.Parameters.Add(intermediatParam);

                    var sensibleParam = command.CreateParameter();
                    sensibleParam.ParameterName = "@sensible";
                    sensibleParam.Value = detail.Sensible;
                    command.Parameters.Add(sensibleParam);

                    var chargeParam = command.CreateParameter();
                    chargeParam.ParameterName = "@charge";
                    chargeParam.Value = detail.Charge ?? string.Empty;
                    command.Parameters.Add(chargeParam);

                    var diameterParam = command.CreateParameter();
                    diameterParam.ParameterName = "@diameter";
                    diameterParam.Value = detail.Diameter ?? string.Empty;
                    command.Parameters.Add(diameterParam);

                    var modifiedByParam = command.CreateParameter();
                    modifiedByParam.ParameterName = "@modifiedBy";
                    modifiedByParam.Value = detail.ModifiedBy ?? (object)DBNull.Value;
                    command.Parameters.Add(modifiedByParam);

                    var modifiedDateParam = command.CreateParameter();
                    modifiedDateParam.ParameterName = "@modifiedDate";
                    modifiedDateParam.Value = DateTime.Now;
                    command.Parameters.Add(modifiedDateParam);

                    var idParam = command.CreateParameter();
                    idParam.ParameterName = "@id";
                    idParam.Value = detail.Id;
                    command.Parameters.Add(idParam);

                    var rowsAffected = await command.ExecuteNonQueryAsync();
                    updateCount += rowsAffected;
                }

                _logger.LogInformation("Batch updated {Count} bacteriology detail records", updateCount);
                return Ok(new { message = "Bacteriology details updated successfully", count = updateCount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error batch updating bacteriology details");
                return StatusCode(500, new { message = "An error occurred while updating bacteriology details", error = ex.Message });
            }
        }
    }

    // Request model for creating bacteriology records
    public class CreateBacteriologyRequest
    {
        public int PatientLabResultId { get; set; }  // ID of PatientLabResult (Antibiogram test with ResultType=3)
        public int GermId { get; set; }
        public int? BacteriaId { get; set; }  // ID of selected bacteria (optional)
        public string? BacteriaName { get; set; }  // Name of selected bacteria (optional)
        public int? CreatedBy { get; set; }
        public string? Comments { get; set; }
        public string? Colony { get; set; }
    }
}

