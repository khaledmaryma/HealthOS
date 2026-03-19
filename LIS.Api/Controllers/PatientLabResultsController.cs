using LIS.Api.Data;
using LIS.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LIS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PatientLabResultsController : ControllerBase
    {
        private readonly LISDbContext _context;
        private readonly ILogger<PatientLabResultsController> _logger;

        public PatientLabResultsController(LISDbContext context, ILogger<PatientLabResultsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all patient lab results by MRN (all admissions)
        /// </summary>
        [HttpGet("byMRN/{mrn:int}")]
        public async Task<ActionResult<IEnumerable<object>>> GetByMRN(
            int mrn,
            [FromQuery] string? currentAdmission = null,
            [FromQuery] int? labTestId = null,
            [FromQuery] int? medicalClassId = null)
        {
            try
            {
                // Join with LabTest to get ResultType and include header info
                var query = from plr in _context.PatientLabResults
                            join plrh in _context.PatientLabResultsHeaders on plr.PatientHeaderID equals plrh.ID
                            join lt in _context.LabTests on plr.LabTestID equals lt.ID into labTestJoin
                            from lt in labTestJoin.DefaultIfEmpty()
                            where plrh.MRN == mrn
                               && plrh.IsDeleted == false
                               && plr.IsDeleted == false
                            select new { plr, plrh, lt };

                // Apply lab test filter if provided
                if (labTestId.HasValue)
                {
                    query = query.Where(x => x.plr.LabTestID == labTestId.Value);
                }

                // Apply medical class filter if provided
                if (medicalClassId.HasValue)
                {
                    query = query.Where(x => x.plr.MedicalClass == medicalClassId.Value);
                }

                var resultsList = await (from x in query
                                     orderby x.plrh.RequestDate descending, x.plr.MedicalClass, x.plr.DisplayOrder
                                     select new
                                     {
                                         x.plr,
                                         x.plrh,
                                         x.lt
                                     }).ToListAsync();

                // Get unique lab test IDs that have age-related references
                var labTestIds = resultsList
                    .Where(x => x.lt != null && x.lt.ReferenceRelatesToAge && x.plr.LabTestID.HasValue)
                    .Select(x => x.plr.LabTestID!.Value)
                    .Distinct()
                    .ToList();

                // Fetch age ranges for these lab tests
                var ageRangesDict = new Dictionary<int, string>();
                if (labTestIds.Any())
                {
                    using var ageCommand = _context.Database.GetDbConnection().CreateCommand();
                    var labTestIdsStr = string.Join(",", labTestIds);
                    ageCommand.CommandText = $@"
                        SELECT ID, LabTest, Description, DefaultMin, DefaultMax, Lower, Higher, DisplayOrder
                        FROM LabTestAge
                        WHERE LabTest IN ({labTestIdsStr}) AND IsDeleted = 0
                        ORDER BY LabTest, Lower";

                    if (ageCommand.Connection!.State != System.Data.ConnectionState.Open)
                    {
                        await ageCommand.Connection.OpenAsync();
                    }

                    using var ageReader = await ageCommand.ExecuteReaderAsync();
                    var ageRangesList = new List<(int LabTest, string? Description, string? DefaultMin, string? DefaultMax, int? Lower, int? Higher)>();
                    
                    while (await ageReader.ReadAsync())
                    {
                        ageRangesList.Add((
                            LabTest: ageReader.GetInt32(1),
                            Description: ageReader.IsDBNull(2) ? null : ageReader.GetString(2),
                            DefaultMin: ageReader.IsDBNull(3) ? null : ageReader.GetString(3),
                            DefaultMax: ageReader.IsDBNull(4) ? null : ageReader.GetString(4),
                            Lower: ageReader.IsDBNull(5) ? null : (int?)ageReader.GetInt32(5),
                            Higher: ageReader.IsDBNull(6) ? null : (int?)ageReader.GetInt32(6)
                        ));
                    }

                    foreach (var ltId in labTestIds)
                    {
                        var ranges = ageRangesList.Where(a => a.LabTest == ltId).ToList();
                        if (ranges.Any())
                        {
                            var rangeStrings = ranges.Select(r =>
                            {
                                var ageDesc = r.Description ?? $"{r.Lower}-{r.Higher}y";
                                var range = "";
                                
                                if (!string.IsNullOrEmpty(r.DefaultMin) && !string.IsNullOrEmpty(r.DefaultMax))
                                {
                                    range = $"{r.DefaultMin} - {r.DefaultMax}";
                                }
                                else if (string.IsNullOrEmpty(r.DefaultMin) && !string.IsNullOrEmpty(r.DefaultMax))
                                {
                                    range = $"<= {r.DefaultMax}";
                                }
                                else if (!string.IsNullOrEmpty(r.DefaultMin) && string.IsNullOrEmpty(r.DefaultMax))
                                {
                                    range = $">= {r.DefaultMin}";
                                }
                                else
                                {
                                    range = "-";
                                }
                                
                                return $"{ageDesc}: {range}";
                            });
                            ageRangesDict[ltId] = string.Join("\n", rangeStrings);
                        }
                    }
                }

                var results = resultsList.Select(x => new
                {
                    id = x.plr.ID,
                    patientHeaderID = x.plr.PatientHeaderID,
                    admissionNumber = x.plrh.AdmissionNumber,
                    requestDate = x.plrh.RequestDate,
                    isCurrentAdmission = x.plrh.AdmissionNumber == currentAdmission,
                    labTestID = x.plr.LabTestID,
                    resultType = x.lt != null ? x.lt.ResultType : (int?)null,
                    referenceRelatesToAge = x.lt != null && x.lt.ReferenceRelatesToAge,
                    ageBasedReferenceRange = x.plr.LabTestID.HasValue && ageRangesDict.ContainsKey(x.plr.LabTestID.Value)
                        ? ageRangesDict[x.plr.LabTestID.Value]
                        : null,
                    labTestDescription = x.plr.LabTestDescription,
                    medicalClass = x.plr.MedicalClass,
                    medicalClassDesc = x.plr.MedicalClassDesc,
                    paragraph = x.plr.Paragraph,
                    min = x.plr.Min,
                    max = x.plr.Max,
                    prefix = x.plr.Prefix,
                    suffix = x.plr.Suffix,
                    errorMin = x.plr.ErrorMin,
                    errorMax = x.plr.ErrorMax,
                    uom = x.plr.UOM,
                    uomDescription = x.plr.UOMDescription,
                    result = x.plr.Result,
                    last = x.plr.Last,
                    lastResultDate = x.plr.LastResultDate,
                    defaultTextResult = x.plr.DefaultTextResult,
                    comments = x.plr.Comments,
                    displayOrder = x.plr.DisplayOrder,
                    statusID = x.plr.StatusID,
                    isResultok = x.plr.IsResultok,
                    guid = x.plr.GUID,
                    resultDate = x.plr.ResultDate,
                    ref_Range = x.plr.Ref_Range,
                    tempHelperID = x.plr.TempHelperID,
                    createdBy = x.plr.CreatedBy,
                    createdDate = x.plr.CreatedDate,
                    modifiedBy = x.plr.ModifiedBy,
                    modifiedDate = x.plr.ModifiedDate,
                    isDeleted = x.plr.IsDeleted,
                    preInvoiceDetailID = x.plr.PreInvoiceDetailID,
                    preInvoiceDetailSequence = x.plr.PreInvoiceDetailSequence,
                    lowPanicIndex = x.plr.LowPanicIndex,
                    highPanicIndex = x.plr.HighPanicIndex,
                    isPanic = x.plr.IsPanic,
                    isNotified = x.plr.IsNotified,
                    panicDate = x.plr.PanicDate,
                    panicComment = x.plr.PanicComment,
                    printed = x.plr.Printed
                }).ToList();

                var filterMsg = "";
                if (labTestId.HasValue) filterMsg += $" (lab test ID: {labTestId.Value})";
                if (medicalClassId.HasValue) filterMsg += $" (medical class ID: {medicalClassId.Value})";
                
                _logger.LogInformation("Retrieved {Count} lab results for MRN {MRN}{Filter}", 
                    results.Count, mrn, filterMsg);

                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving lab results for MRN {MRN}", mrn);
                return StatusCode(500, "An error occurred while retrieving lab results");
            }
        }

        /// <summary>
        /// Get all patient lab results for a specific admission number
        /// </summary>
        [HttpGet("byAdmission/{admissionNumber}")]
        public async Task<ActionResult<IEnumerable<object>>> GetByAdmissionNumber(
            string admissionNumber, 
            [FromQuery] int? labTestId = null,
            [FromQuery] int? medicalClassId = null)
        {
            try
            {
                // Join with LabTest to get ResultType
                var query = from plr in _context.PatientLabResults
                            join plrh in _context.PatientLabResultsHeaders on plr.PatientHeaderID equals plrh.ID
                            join lt in _context.LabTests on plr.LabTestID equals lt.ID into labTestJoin
                            from lt in labTestJoin.DefaultIfEmpty()
                            where plrh.AdmissionNumber == admissionNumber
                               && plrh.IsDeleted == false
                               && plr.IsDeleted == false
                            select new { plr, lt };

                // Apply lab test filter if provided
                if (labTestId.HasValue)
                {
                    query = query.Where(x => x.plr.LabTestID == labTestId.Value);
                }

                // Apply medical class filter if provided
                if (medicalClassId.HasValue)
                {
                    query = query.Where(x => x.plr.MedicalClass == medicalClassId.Value);
                }

                var resultsList = await (from x in query
                                     orderby x.plr.MedicalClass, x.plr.DisplayOrder
                                     select new
                                     {
                                         x.plr,
                                         x.lt
                                     }).ToListAsync();

                // Get unique lab test IDs that have age-related references
                var labTestIds = resultsList
                    .Where(x => x.lt != null && x.lt.ReferenceRelatesToAge && x.plr.LabTestID.HasValue)
                    .Select(x => x.plr.LabTestID!.Value)
                    .Distinct()
                    .ToList();

                // Fetch age ranges for these lab tests using raw SQL to avoid EF Core OPENJSON issues
                var ageRangesDict = new Dictionary<int, string>();
                if (labTestIds.Any())
                {
                    using var ageCommand = _context.Database.GetDbConnection().CreateCommand();
                    var labTestIdsStr = string.Join(",", labTestIds);
                    ageCommand.CommandText = $@"
                        SELECT ID, LabTest, Description, DefaultMin, DefaultMax, Lower, Higher, DisplayOrder
                        FROM LabTestAge
                        WHERE LabTest IN ({labTestIdsStr}) AND IsDeleted = 0
                        ORDER BY LabTest, Lower";

                    if (ageCommand.Connection!.State != System.Data.ConnectionState.Open)
                    {
                        await ageCommand.Connection.OpenAsync();
                    }

                    using var ageReader = await ageCommand.ExecuteReaderAsync();
                    var ageRangesList = new List<(int LabTest, string? Description, string? DefaultMin, string? DefaultMax, int? Lower, int? Higher)>();
                    
                    while (await ageReader.ReadAsync())
                    {
                        ageRangesList.Add((
                            LabTest: ageReader.GetInt32(1),
                            Description: ageReader.IsDBNull(2) ? null : ageReader.GetString(2),
                            DefaultMin: ageReader.IsDBNull(3) ? null : ageReader.GetString(3),
                            DefaultMax: ageReader.IsDBNull(4) ? null : ageReader.GetString(4),
                            Lower: ageReader.IsDBNull(5) ? null : (int?)ageReader.GetInt32(5),
                            Higher: ageReader.IsDBNull(6) ? null : (int?)ageReader.GetInt32(6)
                        ));
                    }

                    foreach (var ltId in labTestIds)
                    {
                        var ranges = ageRangesList.Where(a => a.LabTest == ltId).ToList();
                        if (ranges.Any())
                        {
                            var rangeStrings = ranges.Select(r =>
                            {
                                var ageDesc = r.Description ?? $"{r.Lower}-{r.Higher}y";
                                var range = "";
                                
                                // Build range based on available min/max
                                if (!string.IsNullOrEmpty(r.DefaultMin) && !string.IsNullOrEmpty(r.DefaultMax))
                                {
                                    range = $"{r.DefaultMin} - {r.DefaultMax}";
                                }
                                else if (string.IsNullOrEmpty(r.DefaultMin) && !string.IsNullOrEmpty(r.DefaultMax))
                                {
                                    range = $"<= {r.DefaultMax}";
                                }
                                else if (!string.IsNullOrEmpty(r.DefaultMin) && string.IsNullOrEmpty(r.DefaultMax))
                                {
                                    range = $">= {r.DefaultMin}";
                                }
                                else
                                {
                                    range = "-";
                                }
                                
                                return $"{ageDesc}: {range}";
                            });
                            ageRangesDict[ltId] = string.Join("\n", rangeStrings);
                        }
                    }
                }

                var results = resultsList.Select(x => new
                                     {
                                         id = x.plr.ID,
                                         patientHeaderID = x.plr.PatientHeaderID,
                                         labTestID = x.plr.LabTestID,
                                         resultType = x.lt != null ? x.lt.ResultType : (int?)null,
                    referenceRelatesToAge = x.lt != null && x.lt.ReferenceRelatesToAge,
                    ageBasedReferenceRange = x.plr.LabTestID.HasValue && ageRangesDict.ContainsKey(x.plr.LabTestID.Value)
                        ? ageRangesDict[x.plr.LabTestID.Value]
                        : null,
                                         labTestDescription = x.plr.LabTestDescription,
                                         medicalClass = x.plr.MedicalClass,
                                         medicalClassDesc = x.plr.MedicalClassDesc,
                                         paragraph = x.plr.Paragraph,
                                         min = x.plr.Min,
                                         max = x.plr.Max,
                                         prefix = x.plr.Prefix,
                                         suffix = x.plr.Suffix,
                                         errorMin = x.plr.ErrorMin,
                                         errorMax = x.plr.ErrorMax,
                                         uom = x.plr.UOM,
                                         uomDescription = x.plr.UOMDescription,
                                         result = x.plr.Result,
                                         last = x.plr.Last,
                                         lastResultDate = x.plr.LastResultDate,
                                         defaultTextResult = x.plr.DefaultTextResult,
                                         comments = x.plr.Comments,
                                         displayOrder = x.plr.DisplayOrder,
                                         statusID = x.plr.StatusID,
                                         isResultok = x.plr.IsResultok,
                                         guid = x.plr.GUID,
                                         resultDate = x.plr.ResultDate,
                                         ref_Range = x.plr.Ref_Range,
                                         tempHelperID = x.plr.TempHelperID,
                                         createdBy = x.plr.CreatedBy,
                                         createdDate = x.plr.CreatedDate,
                                         modifiedBy = x.plr.ModifiedBy,
                                         modifiedDate = x.plr.ModifiedDate,
                                         isDeleted = x.plr.IsDeleted,
                                         preInvoiceDetailID = x.plr.PreInvoiceDetailID,
                                         preInvoiceDetailSequence = x.plr.PreInvoiceDetailSequence,
                                         lowPanicIndex = x.plr.LowPanicIndex,
                                         highPanicIndex = x.plr.HighPanicIndex,
                                         isPanic = x.plr.IsPanic,
                                         isNotified = x.plr.IsNotified,
                                         panicDate = x.plr.PanicDate,
                                         panicComment = x.plr.PanicComment,
                                         printed = x.plr.Printed
                }).ToList();

                var filterMsg = "";
                if (labTestId.HasValue) filterMsg += $" (lab test ID: {labTestId.Value})";
                if (medicalClassId.HasValue) filterMsg += $" (medical class ID: {medicalClassId.Value})";
                
                _logger.LogInformation("Retrieved {Count} lab results for admission number {AdmissionNumber}{Filter}", 
                    results.Count, admissionNumber, filterMsg);

                // DEBUG: Log ResultType values for diagnosis
                if (admissionNumber == "03.01254.08.24")
                {
                    _logger.LogWarning("🔍 DEBUG - Results for {AdmissionNumber}:", admissionNumber);
                    foreach (var result in results.Take(10))
                    {
                        var resultType = result.GetType().GetProperty("resultType")?.GetValue(result);
                        var labTestID = result.GetType().GetProperty("labTestID")?.GetValue(result);
                        var description = result.GetType().GetProperty("labTestDescription")?.GetValue(result);
                        _logger.LogWarning("  - ID: {LabTestID}, Type: {ResultType} ({TypeName}), Desc: {Description}", 
                            labTestID, resultType, resultType?.GetType().Name ?? "null", description);
                    }
                }

                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving lab results for admission number {AdmissionNumber}", admissionNumber);
                return StatusCode(500, "An error occurred while retrieving lab results");
            }
        }

        /// <summary>
        /// Get all patient lab results by header ID
        /// </summary>
        [HttpGet("byHeader/{headerId:int}")]
        public async Task<ActionResult<IEnumerable<object>>> GetByHeaderId(int headerId)
        {
            try
            {
                // Join with LabTest to get ResultType
                var resultsList = await (from plr in _context.PatientLabResults
                                     join lt in _context.LabTests on plr.LabTestID equals lt.ID into labTestJoin
                                     from lt in labTestJoin.DefaultIfEmpty()
                                     where plr.PatientHeaderID == headerId && plr.IsDeleted == false
                                     orderby plr.MedicalClass, plr.DisplayOrder
                                         select new { plr, lt }).ToListAsync();

                // Get unique lab test IDs that have age-related references
                var labTestIds = resultsList
                    .Where(x => x.lt != null && x.lt.ReferenceRelatesToAge && x.plr.LabTestID.HasValue)
                    .Select(x => x.plr.LabTestID!.Value)
                    .Distinct()
                    .ToList();

                // Fetch age ranges for these lab tests using raw SQL to avoid EF Core OPENJSON issues
                var ageRangesDict = new Dictionary<int, string>();
                if (labTestIds.Any())
                {
                    using var ageCommand = _context.Database.GetDbConnection().CreateCommand();
                    var labTestIdsStr = string.Join(",", labTestIds);
                    ageCommand.CommandText = $@"
                        SELECT ID, LabTest, Description, DefaultMin, DefaultMax, Lower, Higher, DisplayOrder
                        FROM LabTestAge
                        WHERE LabTest IN ({labTestIdsStr}) AND IsDeleted = 0
                        ORDER BY LabTest, Lower";

                    if (ageCommand.Connection!.State != System.Data.ConnectionState.Open)
                    {
                        await ageCommand.Connection.OpenAsync();
                    }

                    using var ageReader = await ageCommand.ExecuteReaderAsync();
                    var ageRangesList = new List<(int LabTest, string? Description, string? DefaultMin, string? DefaultMax, int? Lower, int? Higher)>();
                    
                    while (await ageReader.ReadAsync())
                    {
                        ageRangesList.Add((
                            LabTest: ageReader.GetInt32(1),
                            Description: ageReader.IsDBNull(2) ? null : ageReader.GetString(2),
                            DefaultMin: ageReader.IsDBNull(3) ? null : ageReader.GetString(3),
                            DefaultMax: ageReader.IsDBNull(4) ? null : ageReader.GetString(4),
                            Lower: ageReader.IsDBNull(5) ? null : (int?)ageReader.GetInt32(5),
                            Higher: ageReader.IsDBNull(6) ? null : (int?)ageReader.GetInt32(6)
                        ));
                    }

                    foreach (var ltId in labTestIds)
                    {
                        var ranges = ageRangesList.Where(a => a.LabTest == ltId).ToList();
                        if (ranges.Any())
                        {
                            var rangeStrings = ranges.Select(r =>
                            {
                                var ageDesc = r.Description ?? $"{r.Lower}-{r.Higher}y";
                                var range = "";
                                
                                // Build range based on available min/max
                                if (!string.IsNullOrEmpty(r.DefaultMin) && !string.IsNullOrEmpty(r.DefaultMax))
                                {
                                    range = $"{r.DefaultMin} - {r.DefaultMax}";
                                }
                                else if (string.IsNullOrEmpty(r.DefaultMin) && !string.IsNullOrEmpty(r.DefaultMax))
                                {
                                    range = $"<= {r.DefaultMax}";
                                }
                                else if (!string.IsNullOrEmpty(r.DefaultMin) && string.IsNullOrEmpty(r.DefaultMax))
                                {
                                    range = $">= {r.DefaultMin}";
                                }
                                else
                                {
                                    range = "-";
                                }
                                
                                return $"{ageDesc}: {range}";
                            });
                            ageRangesDict[ltId] = string.Join("\n", rangeStrings);
                        }
                    }
                }

                var results = resultsList.Select(x => new
                {
                    id = x.plr.ID,
                    patientHeaderID = x.plr.PatientHeaderID,
                    labTestID = x.plr.LabTestID,
                    resultType = x.lt != null ? x.lt.ResultType : (int?)null,
                    referenceRelatesToAge = x.lt != null && x.lt.ReferenceRelatesToAge,
                    ageBasedReferenceRange = x.plr.LabTestID.HasValue && ageRangesDict.ContainsKey(x.plr.LabTestID.Value)
                        ? ageRangesDict[x.plr.LabTestID.Value]
                        : null,
                    labTestDescription = x.plr.LabTestDescription,
                    medicalClass = x.plr.MedicalClass,
                    medicalClassDesc = x.plr.MedicalClassDesc,
                    paragraph = x.plr.Paragraph,
                    min = x.plr.Min,
                    max = x.plr.Max,
                    prefix = x.plr.Prefix,
                    suffix = x.plr.Suffix,
                    errorMin = x.plr.ErrorMin,
                    errorMax = x.plr.ErrorMax,
                    uom = x.plr.UOM,
                    uomDescription = x.plr.UOMDescription,
                    result = x.plr.Result,
                    last = x.plr.Last,
                    lastResultDate = x.plr.LastResultDate,
                    defaultTextResult = x.plr.DefaultTextResult,
                    comments = x.plr.Comments,
                    displayOrder = x.plr.DisplayOrder,
                    statusID = x.plr.StatusID,
                    isResultok = x.plr.IsResultok,
                    guid = x.plr.GUID,
                    resultDate = x.plr.ResultDate,
                    ref_Range = x.plr.Ref_Range,
                    tempHelperID = x.plr.TempHelperID,
                    createdBy = x.plr.CreatedBy,
                    createdDate = x.plr.CreatedDate,
                    modifiedBy = x.plr.ModifiedBy,
                    modifiedDate = x.plr.ModifiedDate,
                    isDeleted = x.plr.IsDeleted,
                    preInvoiceDetailID = x.plr.PreInvoiceDetailID,
                    preInvoiceDetailSequence = x.plr.PreInvoiceDetailSequence,
                    lowPanicIndex = x.plr.LowPanicIndex,
                    highPanicIndex = x.plr.HighPanicIndex,
                    isPanic = x.plr.IsPanic,
                    isNotified = x.plr.IsNotified,
                    panicDate = x.plr.PanicDate,
                    panicComment = x.plr.PanicComment,
                    printed = x.plr.Printed
                }).ToList();

                _logger.LogInformation("Retrieved {Count} lab results for header ID {HeaderId}", results.Count, headerId);
                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving lab results for header ID {HeaderId}", headerId);
                return StatusCode(500, "An error occurred while retrieving lab results");
            }
        }

        /// <summary>
        /// Get a specific patient lab result by ID
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<object>> GetById(int id)
        {
            try
            {
                // Join with LabTest to get ResultType
                var result = await (from plr in _context.PatientLabResults
                                    join lt in _context.LabTests on plr.LabTestID equals lt.ID into labTestJoin
                                    from lt in labTestJoin.DefaultIfEmpty()
                                    where plr.ID == id && plr.IsDeleted == false
                                    select new
                                    {
                                        id = plr.ID,
                                        patientHeaderID = plr.PatientHeaderID,
                                        labTestID = plr.LabTestID,
                                        resultType = lt != null ? lt.ResultType : (int?)null,
                                        labTestDescription = plr.LabTestDescription,
                                        medicalClass = plr.MedicalClass,
                                        medicalClassDesc = plr.MedicalClassDesc,
                                        paragraph = plr.Paragraph,
                                        min = plr.Min,
                                        max = plr.Max,
                                        prefix = plr.Prefix,
                                        suffix = plr.Suffix,
                                        errorMin = plr.ErrorMin,
                                        errorMax = plr.ErrorMax,
                                        uom = plr.UOM,
                                        uomDescription = plr.UOMDescription,
                                        result = plr.Result,
                                        last = plr.Last,
                                        lastResultDate = plr.LastResultDate,
                                        defaultTextResult = plr.DefaultTextResult,
                                        comments = plr.Comments,
                                        displayOrder = plr.DisplayOrder,
                                        statusID = plr.StatusID,
                                        isResultok = plr.IsResultok,
                                        guid = plr.GUID,
                                        resultDate = plr.ResultDate,
                                        ref_Range = plr.Ref_Range,
                                        tempHelperID = plr.TempHelperID,
                                        createdBy = plr.CreatedBy,
                                        createdDate = plr.CreatedDate,
                                        modifiedBy = plr.ModifiedBy,
                                        modifiedDate = plr.ModifiedDate,
                                        isDeleted = plr.IsDeleted,
                                        preInvoiceDetailID = plr.PreInvoiceDetailID,
                                        preInvoiceDetailSequence = plr.PreInvoiceDetailSequence,
                                        lowPanicIndex = plr.LowPanicIndex,
                                        highPanicIndex = plr.HighPanicIndex,
                                        isPanic = plr.IsPanic,
                                        isNotified = plr.IsNotified,
                                        panicDate = plr.PanicDate,
                                        panicComment = plr.PanicComment,
                                        printed = plr.Printed
                                    }).FirstOrDefaultAsync();

                if (result == null)
                {
                    _logger.LogWarning("Patient lab result with ID {Id} not found", id);
                    return NotFound(new { message = $"Patient lab result with ID {id} not found" });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving patient lab result {Id}", id);
                return StatusCode(500, "An error occurred while retrieving the patient lab result");
            }
        }

        /// <summary>
        /// Update a patient lab result
        /// </summary>
        [HttpPut("{id:int}")]
        public async Task<ActionResult<PatientLabResult>> Update(int id, [FromBody] PatientLabResult input)
        {
            try
            {
                if (id != input.ID)
                {
                    return BadRequest("ID mismatch");
                }

                var existing = await _context.PatientLabResults
                    .FirstOrDefaultAsync(r => r.ID == id && r.IsDeleted == false);

                if (existing == null)
                {
                    _logger.LogWarning("Patient lab result with ID {Id} not found for update", id);
                    return NotFound(new { message = $"Patient lab result with ID {id} not found" });
                }

                // Update only the editable fields
                existing.Min = input.Min;
                existing.Max = input.Max;
                existing.Result = input.Result;
                existing.ModifiedDate = DateTime.Now;
                // Note: ModifiedBy should be set from the authenticated user context
                // For now, we'll leave it as is or you can add user context later

                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated patient lab result {Id}", id);
                return Ok(existing);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating patient lab result {Id}", id);
                return StatusCode(500, "An error occurred while updating the patient lab result");
            }
        }

        /// <summary>
        /// Generate lab results from patient invoice for a specific admission
        /// </summary>
        [HttpPost("generateFromInvoice/{admissionNumber}")]
        public async Task<ActionResult<object>> GenerateFromInvoice(string admissionNumber)
        {
            try
            {
                _logger.LogInformation("Starting invoice-based lab result generation for admission {AdmissionNumber}", admissionNumber);
                // Step 1: Get patient invoice from InvoiceHeader
                using var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();
                
                using var invoiceCommand = connection.CreateCommand();
                invoiceCommand.CommandTimeout = 30; // 30 seconds timeout
                invoiceCommand.CommandText = @"
                    SELECT TOP 1 ID, Counter, MRN, AdmissionNumber, Date, Net
                    FROM Billing.dbo.InvoiceHeader WITH (NOLOCK)
                    WHERE AdmissionNumber = @admissionNumber AND IsDeleted = 0
                    ORDER BY Date DESC";
                
                var admissionParam = invoiceCommand.CreateParameter();
                admissionParam.ParameterName = "@admissionNumber";
                admissionParam.Value = admissionNumber;
                invoiceCommand.Parameters.Add(admissionParam);
                
                using var invoiceReader = await invoiceCommand.ExecuteReaderAsync();
                if (!await invoiceReader.ReadAsync())
                {
                    _logger.LogWarning("No invoice found for admission number {AdmissionNumber}", admissionNumber);
                    return NotFound(new { message = $"No invoice found for admission number {admissionNumber}" });
                }
                
                var invoiceId = invoiceReader.GetInt32(0);
                var invoiceNumber = invoiceReader.IsDBNull(1) ? null : invoiceReader.GetString(1);
                var patientId = invoiceReader.IsDBNull(2) ? null : invoiceReader.GetString(2);
                var invoiceDate = invoiceReader.IsDBNull(4) ? DateTime.Now : invoiceReader.GetDateTime(4);
                
                invoiceReader.Close();
                
                // Step 2: Get invoice details with costcenter = 1 to find lab test denominations
                using var detailCommand = connection.CreateCommand();
                detailCommand.CommandTimeout = 30; // 30 seconds timeout
                detailCommand.CommandText = @"
                    SELECT ID, InvoiceHeader, Denomination, DenominationDescription, Quantity, UnitPrice, NetPrice
                    FROM Billing.dbo.InvoiceDetail WITH (NOLOCK)
                    WHERE InvoiceHeader = @invoiceId AND CostCenter = 1 AND IsDeleted = 0
                    ORDER BY ID";
                
                var invoiceIdParam = detailCommand.CreateParameter();
                invoiceIdParam.ParameterName = "@invoiceId";
                invoiceIdParam.Value = invoiceId;
                detailCommand.Parameters.Add(invoiceIdParam);
                
                using var detailReader = await detailCommand.ExecuteReaderAsync();
                var invoiceDetails = new List<(int Id, int InvoiceHeaderId, int? Denomination, string? ItemDescription, decimal? Quantity, decimal? UnitPrice, decimal? TotalPrice)>();
                
                while (await detailReader.ReadAsync())
                {
                    invoiceDetails.Add((
                        Id: detailReader.GetInt32(0),
                        InvoiceHeaderId: detailReader.GetInt32(1),
                        Denomination: detailReader.IsDBNull(2) ? null : (int?)detailReader.GetInt32(2),
                        ItemDescription: detailReader.IsDBNull(3) ? null : detailReader.GetString(3),
                        Quantity: detailReader.IsDBNull(4) ? null : (decimal?)detailReader.GetDecimal(4),
                        UnitPrice: detailReader.IsDBNull(5) ? null : (decimal?)detailReader.GetDecimal(5),
                        TotalPrice: detailReader.IsDBNull(6) ? null : (decimal?)detailReader.GetDecimal(6)
                    ));
                }
                detailReader.Close();
                
                if (!invoiceDetails.Any())
                {
                    _logger.LogWarning("No lab test items found in invoice {InvoiceId} for admission {AdmissionNumber}", invoiceId, admissionNumber);
                    return NotFound(new { message = $"No lab test items found in invoice for admission {admissionNumber}" });
                }
                
                // Step 3: Get denominations and corresponding lab tests
                _logger.LogInformation("Processing {Count} invoice details for admission {AdmissionNumber}", invoiceDetails.Count, admissionNumber);
                
                var denominations = invoiceDetails.Where(d => d.Denomination.HasValue).Select(d => d.Denomination!.Value).Distinct().ToList();
                _logger.LogInformation("Extracted denominations from invoice: [{Denominations}]", string.Join(", ", denominations));
                
                if (!denominations.Any())
                {
                    _logger.LogWarning("No denominations found in invoice details for admission {AdmissionNumber}", admissionNumber);
                    return NotFound(new { message = $"No denominations found in invoice for admission {admissionNumber}" });
                }
                
                // Get lab tests for these denominations using raw SQL to avoid EF Core OPENJSON issues
                _logger.LogInformation("Fetching lab tests for denominations...");
                var denominationsStr = string.Join(",", denominations);
                var labTests = await _context.LabTests
                    .FromSqlRaw($"SELECT * FROM [LabTest] WHERE COALESCE([Denomination], 0) IN ({denominationsStr}) AND [IsDeleted] = 0")
                    .ToListAsync();
                
                _logger.LogInformation("Found {Count} lab tests matching the denominations", labTests.Count);
                _logger.LogInformation("Lab Tests from LabTest table:");
                foreach (var lt in labTests)
                {
                    _logger.LogInformation("  - ID: {Id}, Description: {Description}, Denomination: {Denomination}, Medical Class: {MedicalClass}", 
                        lt.ID, lt.TestDesciption, lt.Denomination, lt.MedicalClass);
                }
                
                if (!labTests.Any())
                {
                    _logger.LogWarning("No lab tests found for denominations {Denominations} for admission {AdmissionNumber}", 
                        string.Join(",", denominations), admissionNumber);
                    return NotFound(new { message = $"No lab tests found for denominations in invoice for admission {admissionNumber}" });
                }
                
                // Step 4: Check if patient lab results header exists for this admission
                var existingHeader = await _context.PatientLabResultsHeaders
                    .FirstOrDefaultAsync(h => h.AdmissionNumber == admissionNumber && !h.IsDeleted);
                
                PatientLabResultsHeader header;
                if (existingHeader == null)
                {
                    // Create new header
                    header = new PatientLabResultsHeader
                    {
                        MRN = int.TryParse(patientId, out int mrn) ? mrn : 0,
                        AdmissionNumber = admissionNumber,
                        RequestDate = invoiceDate,
                        CreatedDate = DateTime.Now,
                        CreatedBy = -1, // Default system user
                        IsDeleted = false
                    };
                    _context.PatientLabResultsHeaders.Add(header);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Created new patient lab results header {HeaderId} for admission {AdmissionNumber}", header.ID, admissionNumber);
                }
                else
                {
                    header = existingHeader;
                    _logger.LogInformation("Using existing patient lab results header {HeaderId} for admission {AdmissionNumber}", header.ID, admissionNumber);
                }
                
                // Step 5: Check existing lab results and create new ones for missing lab tests
                _logger.LogInformation("====== DUPLICATION CHECK START ======");
                _logger.LogInformation("Checking for existing lab results for Header ID: {HeaderId}, Admission: {AdmissionNumber}", 
                    header.ID, admissionNumber);
                
                var existingResults = await _context.PatientLabResults
                    .Where(r => r.PatientHeaderID == header.ID && !r.IsDeleted)
                    .ToListAsync();
                
                _logger.LogInformation("Found {Count} existing lab results in database", existingResults.Count);
                
                var existingLabTestIds = existingResults.Where(r => r.LabTestID.HasValue).Select(r => r.LabTestID!.Value).ToHashSet();
                _logger.LogInformation("Existing Lab Test IDs: [{TestIds}]", string.Join(", ", existingLabTestIds));
                
                var invoiceLabTestIds = labTests.Select(lt => lt.ID).ToList();
                _logger.LogInformation("Invoice Lab Test IDs (from denominations): [{TestIds}]", string.Join(", ", invoiceLabTestIds));
                
                var newLabTests = labTests.Where(lt => !existingLabTestIds.Contains(lt.ID)).ToList();
                _logger.LogInformation("New Lab Tests to create: {Count}", newLabTests.Count);
                
                if (newLabTests.Any())
                {
                    _logger.LogInformation("New Lab Test IDs to be created: [{TestIds}]", 
                        string.Join(", ", newLabTests.Select(lt => lt.ID)));
                    _logger.LogInformation("New Lab Test Details:");
                    foreach (var lt in newLabTests)
                    {
                        _logger.LogInformation("  - ID: {Id}, Description: {Description}, Denomination: {Denomination}, Medical Class: {MedicalClass}", 
                            lt.ID, lt.TestDesciption, lt.Denomination, lt.MedicalClass);
                    }
                }
                
                if (existingResults.Any())
                {
                    _logger.LogInformation("Existing Lab Results Details:");
                    foreach (var er in existingResults)
                    {
                        _logger.LogInformation("  - Result ID: {ResultId}, Lab Test ID: {LabTestId}, Description: {Description}", 
                            er.ID, er.LabTestID, er.LabTestDescription);
                    }
                }
                
                var duplicateCount = labTests.Count - newLabTests.Count;
                _logger.LogInformation("Duplicate Prevention Summary: {DuplicateCount} lab tests already exist, {NewCount} new tests will be created", 
                    duplicateCount, newLabTests.Count);
                
                var createdResults = new List<PatientLabResult>();
                
                // Batch create all new results at once for better performance
                foreach (var labTest in newLabTests)
                {
                    var invoiceDetail = invoiceDetails.FirstOrDefault(d => d.Denomination == labTest.Denomination);
                    
                    var newResult = new PatientLabResult
                    {
                        PatientHeaderID = header.ID,
                        LabTestID = labTest.ID,
                        LabTestDescription = labTest.TestDesciption ?? "",
                        MedicalClass = labTest.MedicalClass ?? 0,
                        MedicalClassDesc = labTest.MedicalClassDescription ?? "",
                        Paragraph = "",
                        Min = labTest.DefaultNoramlMin,
                        Max = labTest.DefaultNormalMax,
                        Prefix = labTest.Prefix,
                        Suffix = labTest.Suffix,
                        ErrorMin = labTest.ErrorRangeMin?.ToString(),
                        ErrorMax = labTest.ErrorRangeMax?.ToString(),
                        UOM = labTest.UOM,
                        UOMDescription = "", // Will be populated from UnitOfMeasure if needed
                        Result = null, // Empty result for user to enter
                        Last = null,
                        LastResultDate = null,
                        DefaultTextResult = labTest.DefaultTextResult,
                        Comments = null,
                        DisplayOrder = labTest.DisplayOrder,
                        StatusID = null,
                        IsResultok = false,
                        GUID = Guid.NewGuid(),
                        ResultDate = null,
                        Ref_Range = null,
                        TempHelperID = null,
                        CreatedBy = -1, // Default system user
                        CreatedDate = DateTime.Now,
                        ModifiedBy = null,
                        ModifiedDate = null,
                        IsDeleted = false,
                        PreInvoiceDetailID = invoiceDetail.Id,
                        PreInvoiceDetailSequence = null,
                        LowPanicIndex = labTest.LowPanicIndex,
                        HighPanicIndex = labTest.HighPanicIndex,
                        IsPanic = false,
                        IsNotified = false,
                        PanicDate = null,
                        PanicComment = null,
                        Printed = false
                    };
                    
                    createdResults.Add(newResult);
                }
                
                // Add all results in batch
                if (createdResults.Any())
                {
                    _logger.LogInformation("Saving {Count} new lab results to database...", createdResults.Count);
                    _context.PatientLabResults.AddRange(createdResults);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Successfully saved {Count} new lab results", createdResults.Count);
                    
                    _logger.LogInformation("Created Lab Results Summary:");
                    foreach (var cr in createdResults)
                    {
                        _logger.LogInformation("  - Result ID: {ResultId}, Lab Test ID: {LabTestId}, Description: {Description}, PreInvoiceDetailID: {InvoiceDetailId}", 
                            cr.ID, cr.LabTestID, cr.LabTestDescription, cr.PreInvoiceDetailID);
                    }
                }
                else
                {
                    _logger.LogInformation("No new lab results to save - all tests from invoice already exist");
                }
                
                _logger.LogInformation("====== DUPLICATION CHECK END ======");
                _logger.LogInformation("Generated {Count} new lab results for admission {AdmissionNumber} from invoice {InvoiceId}", 
                    createdResults.Count, admissionNumber, invoiceId);
                
                return Ok(new
                {
                    message = $"Generated {createdResults.Count} new lab results from invoice",
                    admissionNumber = admissionNumber,
                    invoiceId = invoiceId,
                    invoiceNumber = invoiceNumber,
                    headerId = header.ID,
                    existingResultsCount = existingResults.Count,
                    newResultsCount = createdResults.Count,
                    denominations = denominations,
                    labTestsGenerated = createdResults.Select(r => new
                    {
                        id = r.ID,
                        labTestId = r.LabTestID,
                        labTestDescription = r.LabTestDescription,
                        denomination = labTests.FirstOrDefault(lt => lt.ID == r.LabTestID)?.Denomination
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating lab results from invoice for admission {AdmissionNumber}", admissionNumber);
                return StatusCode(500, new { message = "An error occurred while generating lab results from invoice", error = ex.Message });
            }
        }

        /// <summary>
        /// Batch update multiple patient lab results
        /// </summary>
        [HttpPut("batch")]
        public async Task<ActionResult> BatchUpdate([FromBody] List<PatientLabResult> results)
        {
            try
            {
                // Use raw SQL to avoid trigger issues with EF Core's OUTPUT clause
                var updateCount = 0;
                
                using var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();
                
                foreach (var input in results)
                {
                    using var command = connection.CreateCommand();
                    command.CommandText = @"
                        UPDATE PatientLabResult 
                        SET Min = @min, 
                            Max = @max, 
                            Result = @result, 
                            ModifiedDate = @modifiedDate
                        WHERE ID = @id AND IsDeleted = 0";
                    
                    var minParam = command.CreateParameter();
                    minParam.ParameterName = "@min";
                    minParam.Value = input.Min ?? (object)DBNull.Value;
                    command.Parameters.Add(minParam);
                    
                    var maxParam = command.CreateParameter();
                    maxParam.ParameterName = "@max";
                    maxParam.Value = input.Max ?? (object)DBNull.Value;
                    command.Parameters.Add(maxParam);
                    
                    var resultParam = command.CreateParameter();
                    resultParam.ParameterName = "@result";
                    resultParam.Value = input.Result ?? (object)DBNull.Value;
                    command.Parameters.Add(resultParam);
                    
                    var modifiedDateParam = command.CreateParameter();
                    modifiedDateParam.ParameterName = "@modifiedDate";
                    modifiedDateParam.Value = DateTime.Now;
                    command.Parameters.Add(modifiedDateParam);
                    
                    var idParam = command.CreateParameter();
                    idParam.ParameterName = "@id";
                    idParam.Value = input.ID;
                    command.Parameters.Add(idParam);
                    
                    var rowsAffected = await command.ExecuteNonQueryAsync();
                    updateCount += rowsAffected;
                }

                _logger.LogInformation("Batch updated {Count} patient lab results", updateCount);
                return Ok(new { message = "Results updated successfully", count = updateCount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error batch updating patient lab results");
                return StatusCode(500, new { message = "An error occurred while updating the results", error = ex.Message });
            }
        }
    }
}

