using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LIS.Api.Data;
using LIS.Api.Models;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Linq;

namespace LIS.Api.Controllers
{
    [ApiController]
    [Route("api/HospitalDefinition/[controller]")]
    public class DenominationController : ControllerBase
    {
        private readonly HospitalDefinitionDbContext _context;
        private readonly ILogger<DenominationController> _logger;
        private readonly IConfiguration _configuration;

        public DenominationController(HospitalDefinitionDbContext context, ILogger<DenominationController> logger, IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<HospitalDenomination>>> GetDenominations()
        {
            try
            {
                // Return test data for now
                var denominations = new List<HospitalDenomination>
                {
                    new HospitalDenomination
                    {
                        Id = 1,
                        SmallDescription = "Blood Test - Complete",
                        LongDescription = "Complete Blood Count Test",
                        Code = "CBC001",
                        CoefficientValue = 25.50m,
                        Status = 3,
                        DisplayOrder = "1",
                        IsDeleted = 0,
                        CreatedDate = DateTime.Now,
                        HasOperatingPhysician = 0,
                        HasAnesthesiaPhysician = 0,
                        HasOperatingRoom = 0,
                        IsHonoraryExcluded = 0,
                        IsResidenceRelated = 0,
                        HasMedicalResult = 1,
                        App = 1,
                        CoefficientCode = "CBC",
                        CostCenter = "12",
                        IsSubItem = 0,
                        CreatedBy = 298,
                        IsSelectedOrNot = 0,
                        HasVideo = 0,
                        IsOpenHeart = 0,
                        IsPrintable = 1
                    },
                    new HospitalDenomination
                    {
                        Id = 2,
                        SmallDescription = "X-Ray - Chest",
                        LongDescription = "Chest X-Ray Examination",
                        Code = "XRAY001",
                        CoefficientValue = 45.00m,
                        Status = 3,
                        DisplayOrder = "2",
                        IsDeleted = 0,
                        CreatedDate = DateTime.Now,
                        HasOperatingPhysician = 0,
                        HasAnesthesiaPhysician = 0,
                        HasOperatingRoom = 0,
                        IsHonoraryExcluded = 0,
                        IsResidenceRelated = 0,
                        HasMedicalResult = 1,
                        App = 1,
                        CoefficientCode = "XRAY",
                        CostCenter = "12",
                        IsSubItem = 0,
                        CreatedBy = 298,
                        IsSelectedOrNot = 0,
                        HasVideo = 0,
                        IsOpenHeart = 0,
                        IsPrintable = 1
                    }
                };

                return Ok(denominations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving denominations");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<HospitalDenomination>> GetDenomination(int id)
        {
            try
            {
                // Use raw SQL for consistency with GetDenominationsForQuickAdmission/Search (avoids EF schema issues)
                var sqlQuery = @"
                    SELECT 
                        [ID] AS [Id],
                        COALESCE([SmallDescription], '') AS [SmallDescription],
                        COALESCE([LongDescription], '') AS [LongDescription],
                        COALESCE([Code], '') AS [Code],
                        COALESCE([Abreviation], '') AS [Abreviation],
                        COALESCE([HasOperatingPhysician], 0) AS [HasOperatingPhysician],
                        COALESCE([HasAnesthesiaPhysician], 0) AS [HasAnesthesiaPhysician],
                        COALESCE([HasOperatingRoom], 0) AS [HasOperatingRoom],
                        COALESCE([IsHonoraryExcluded], 0) AS [IsHonoraryExcluded],
                        COALESCE([IsResidenceRelated], 0) AS [IsResidenceRelated],
                        COALESCE([HasMedicalResult], 0) AS [HasMedicalResult],
                        COALESCE([App], 0) AS [App],
                        COALESCE(CAST([OperatingRoom] AS NVARCHAR(MAX)), '') AS [OperatingRoom],
                        COALESCE([CoefficientCode], '') AS [CoefficientCode],
                        COALESCE([CoefficientValue], 0) AS [CoefficientValue],
                        COALESCE([CashPriceUsd], 0) AS [CashPriceUsd],
                        COALESCE([CashPriceLlbp], 0) AS [CashPriceLlbp],
                        COALESCE([Status], 0) AS [Status],
                        COALESCE(CAST([DisplayOrder] AS NVARCHAR(MAX)), '') AS [DisplayOrder],
                        COALESCE(CAST([CostCenter] AS NVARCHAR(MAX)), '') AS [CostCenter],
                        COALESCE([ExpectedResidenceDays], 0) AS [ExpectedResidenceDays],
                        COALESCE([IsSubItem], 0) AS [IsSubItem],
                        COALESCE([IsDeleted], 0) AS [IsDeleted],
                        COALESCE([CreatedBy], 0) AS [CreatedBy],
                        COALESCE([ModifiedBy], 0) AS [ModifiedBy],
                        COALESCE([CreatedDate], GETDATE()) AS [CreatedDate],
                        NULL AS [ModifiedDate],
                        COALESCE([StartDate], 0) AS [StartDate],
                        COALESCE([StartDateLabel], 0) AS [StartDateLabel],
                        COALESCE([EndDate], 0) AS [EndDate],
                        COALESCE([EndDateLabel], 0) AS [EndDateLabel],
                        COALESCE([IsSelectedOrNot], 0) AS [IsSelectedOrNot],
                        COALESCE([SeverityID], 0) AS [SeverityId],
                        COALESCE([StatusID], 0) AS [StatusId],
                        COALESCE([Comments], '') AS [Comments],
                        COALESCE([InCrAppCode], '') AS [InCrAppCode],
                        COALESCE([InCaAppCode], '') AS [InCaAppCode],
                        COALESCE([OutCrAppCode], '') AS [OutCrAppCode],
                        COALESCE([OutCaAppCode], '') AS [OutCaAppCode],
                        COALESCE([DenominationDefaultTime], 0) AS [DenominationDefaultTime],
                        COALESCE([Rate], 0) AS [Rate],
                        COALESCE([HasVideo], 0) AS [HasVideo],
                        COALESCE([IsOpenHeart], 0) AS [IsOpenHeart],
                        COALESCE([IsReferralShare], 0) AS [IsReferralShare],
                        COALESCE([ReferralAmount], 0) AS [ReferralAmount],
                        COALESCE([DenominationGroupID], 0) AS [DenominationGroupId],
                        COALESCE([IsClassRelated], 0) AS [IsClassRelated],
                        COALESCE([CreditDiscount], '') AS [CreditDiscount],
                        COALESCE([CashDiscount], '') AS [CashDiscount],
                        COALESCE([IsPrintable], 0) AS [IsPrintable]
                    FROM [dbo].[Denomination]
                    WHERE [ID] = {0} AND COALESCE([IsDeleted], 0) = 0";

                var denomination = await _context.Database
                    .SqlQueryRaw<HospitalDenomination>(sqlQuery, id)
                    .FirstOrDefaultAsync();

                if (denomination == null)
                {
                    return NotFound();
                }

                return Ok(denomination);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving denomination {Id}. Details: {Details}", id, ex.InnerException?.Message);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("SearchAdvanced")]
        public async Task<ActionResult<IEnumerable<DenominationSearchResult>>> SearchDenominationsAdvanced(
            [FromQuery] string? searchQuery = null,
            [FromQuery] int insuranceId = 5,
            [FromQuery] string? costCenterIds = null)
        {
            try
            {
                    var connectionString = _configuration.GetConnectionString("HospitalDefinitionConnection");
                    if (string.IsNullOrEmpty(connectionString))
                    {
                        _logger.LogError("HospitalDefinitionConnection string is not configured");
                        return StatusCode(500, new { message = "Database connection string not configured" });
                    }

                    var results = new List<DenominationSearchResult>();

                    using (var connection = new SqlConnection(connectionString))
                    {
                        await connection.OpenAsync();
                        _logger.LogInformation("Connected to HospitalDefinition database. Starting denomination search with InsuranceId={InsuranceId}, CostCenterIds={CostCenterIds}, SearchQuery={SearchQuery}", 
                            insuranceId, costCenterIds, searchQuery);

                    // Build WHERE clause
                    var whereConditions = new List<string>
                    {
                        "ISNULL(d.isdeleted,0) = 0",
                        "ISNULL(ccp.isdeleted,0) = 0",
                        "ISNULL(cp.isdeleted,0) = 0",
                        "ISNULL(ins.isdeleted,0) = 0",
                        "ISNULL(cc.isdeleted,0) = 0",
                        "ISNULL(lb.isdeleted,0) = 0",
                        $"ins.id = @insuranceId"
                    };

                    // Cost center filter
                    if (!string.IsNullOrWhiteSpace(costCenterIds))
                    {
                        var costCenterList = costCenterIds.Split(',')
                            .Select(cc => cc.Trim())
                            .Where(cc => !string.IsNullOrEmpty(cc) && int.TryParse(cc, out _))
                            .ToList();
                        
                        if (costCenterList.Any())
                        {
                            var costCenterInClause = string.Join(",", costCenterList);
                            whereConditions.Add($"d.costcenter IN ({costCenterInClause})");
                        }
                    }

                    // Search query filter
                    if (!string.IsNullOrWhiteSpace(searchQuery))
                    {
                        whereConditions.Add("(d.smalldescription LIKE @searchQuery OR d.code LIKE @searchQuery OR lb.TestDesciption LIKE @searchQuery)");
                    }

                    var whereClause = string.Join(" AND ", whereConditions);

                    var query = $@"
                        SELECT  
                            ins.Description as Insurance, 
                            ins.Id as InsId, 
                            cc.Description as CostCenterName, 
                            cc.id as CostCenterId, 
                            d.id as DenId, 
                            d.code as ActCode,
                            d.smalldescription as ActName, 
                            lb.TestDesciption as LabTest, 
                            d.coefficientvalue as CoefficientValue,
                            ISNULL(ccp.outlbp,0) as OutLL,
                            ISNULL(ccp.outusd,0) as OutUsd,
                            d.coefficientvalue * ISNULL(ccp.outlbp,0) as PriceLL,
                                                                                                                                                                d.coefficientvalue * ISNULL(ccp.outusd,0) as PriceUsd,
                            d.HasOperatingPhysician
                        FROM HospitalDefinition.dbo.denomination as d 
                        INNER JOIN HospitalDefinition.dbo.costcenterprice as ccp ON d.costcenter = ccp.costcenter
                        INNER JOIN HospitalDefinition.dbo.contextprice as cp ON cp.id = ccp.contextprice
                        INNER JOIN HospitalDefinition.dbo.insurance as ins ON cp.defaultinsurance = ins.id
                        INNER JOIN HospitalDefinition.dbo.costcenter as cc ON d.costcenter = cc.id
                        LEFT JOIN LIS.dbo.labtest as lb ON d.id = lb.Denomination AND (lb.IsDeleted = 0 OR lb.IsDeleted IS NULL)
                        WHERE {whereClause}
                        ORDER BY d.smalldescription";

                    using (var command = new SqlCommand(query, connection))
                    {
                        // Add insurance parameter
                        command.Parameters.AddWithValue("@insuranceId", insuranceId);

                        // Add search query parameter
                        if (!string.IsNullOrWhiteSpace(searchQuery))
                        {
                            command.Parameters.AddWithValue("@searchQuery", $"%{searchQuery}%");
                        }

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            // Get column ordinals once before the loop
                            int insuranceOrdinal, insIdOrdinal, costCenterNameOrdinal, costCenterIdOrdinal, denIdOrdinal;
                            int actCodeOrdinal, actNameOrdinal, labTestOrdinal, coefficientValueOrdinal;
                            int outLLOrdinal, outUsdOrdinal, priceLLOrdinal, priceUsdOrdinal, hasOperatingPhysicianOrdinal;

                            try
                            {
                                insuranceOrdinal = reader.GetOrdinal("Insurance");
                                insIdOrdinal = reader.GetOrdinal("InsId");
                                costCenterNameOrdinal = reader.GetOrdinal("CostCenterName");
                                costCenterIdOrdinal = reader.GetOrdinal("CostCenterId");
                                denIdOrdinal = reader.GetOrdinal("DenId");
                                actCodeOrdinal = reader.GetOrdinal("ActCode");
                                actNameOrdinal = reader.GetOrdinal("ActName");
                                labTestOrdinal = reader.GetOrdinal("LabTest");
                                coefficientValueOrdinal = reader.GetOrdinal("CoefficientValue");
                                outLLOrdinal = reader.GetOrdinal("OutLL");
                                outUsdOrdinal = reader.GetOrdinal("OutUsd");
                                priceLLOrdinal = reader.GetOrdinal("PriceLL");
                                priceUsdOrdinal = reader.GetOrdinal("PriceUsd");
                                hasOperatingPhysicianOrdinal = reader.GetOrdinal("HasOperatingPhysician");
                            }
                            catch (Exception colEx)
                            {
                                _logger.LogError(colEx, "Error getting column ordinals from query result");
                                return StatusCode(500, new { message = "Error reading query result columns", error = colEx.Message });
                            }

                            while (await reader.ReadAsync())
                            {
                                try
                                {

                                    // Safely read all values with proper null handling using GetValue
                                    var insurance = reader.IsDBNull(insuranceOrdinal) ? null : reader.GetString(insuranceOrdinal);
                                    var insId = reader.IsDBNull(insIdOrdinal) ? 0 : Convert.ToInt32(reader.GetValue(insIdOrdinal));
                                    var costCenterName = reader.IsDBNull(costCenterNameOrdinal) ? null : reader.GetString(costCenterNameOrdinal);
                                    var costCenterId = reader.IsDBNull(costCenterIdOrdinal) ? 0 : Convert.ToInt32(reader.GetValue(costCenterIdOrdinal));
                                    var denId = reader.IsDBNull(denIdOrdinal) ? 0 : Convert.ToInt32(reader.GetValue(denIdOrdinal));
                                    var actCode = reader.IsDBNull(actCodeOrdinal) ? null : reader.GetString(actCodeOrdinal);
                                    var actName = reader.IsDBNull(actNameOrdinal) ? null : reader.GetString(actNameOrdinal);
                                    var labTest = reader.IsDBNull(labTestOrdinal) ? null : reader.GetString(labTestOrdinal);
                                    var coefficientValue = reader.IsDBNull(coefficientValueOrdinal) ? (decimal?)null : Convert.ToDecimal(reader.GetValue(coefficientValueOrdinal));
                                    var outLL = reader.IsDBNull(outLLOrdinal) ? 0m : Convert.ToDecimal(reader.GetValue(outLLOrdinal));
                                    var outUsd = reader.IsDBNull(outUsdOrdinal) ? 0m : Convert.ToDecimal(reader.GetValue(outUsdOrdinal));
                                    var priceLL = reader.IsDBNull(priceLLOrdinal) ? 0m : Convert.ToDecimal(reader.GetValue(priceLLOrdinal));
                                    var priceUsd = reader.IsDBNull(priceUsdOrdinal) ? 0m : Convert.ToDecimal(reader.GetValue(priceUsdOrdinal));
                                    
                                    // Handle HasOperatingPhysician - can be bool, int, or null
                                    bool? hasOperatingPhysician = null;
                                    if (!reader.IsDBNull(hasOperatingPhysicianOrdinal))
                                    {
                                        try
                                        {
                                            var fieldType = reader.GetFieldType(hasOperatingPhysicianOrdinal);
                                            if (fieldType == typeof(bool))
                                            {
                                                hasOperatingPhysician = reader.GetBoolean(hasOperatingPhysicianOrdinal);
                                            }
                                            else if (fieldType == typeof(int) || fieldType == typeof(short) || fieldType == typeof(byte))
                                            {
                                                var intValue = Convert.ToInt32(reader.GetValue(hasOperatingPhysicianOrdinal));
                                                hasOperatingPhysician = intValue == 1;
                                            }
                                            else
                                            {
                                                // Try to convert to bool
                                                var value = reader.GetValue(hasOperatingPhysicianOrdinal);
                                                if (value != null)
                                                {
                                                    hasOperatingPhysician = Convert.ToBoolean(value);
                                                }
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            _logger.LogWarning(ex, "Error reading HasOperatingPhysician field, defaulting to null. DenId: {DenId}", denId);
                                            hasOperatingPhysician = null;
                                        }
                                    }

                                    results.Add(new DenominationSearchResult
                                    {
                                        Insurance = insurance,
                                        InsId = insId,
                                        CostCenterName = costCenterName,
                                        CostCenterId = costCenterId,
                                        DenId = denId,
                                        ActCode = actCode,
                                        ActName = actName,
                                        LabTest = labTest,
                                        CoefficientValue = coefficientValue,
                                        OutLL = outLL,
                                        OutUsd = outUsd,
                                        PriceLL = priceLL,
                                        PriceUsd = priceUsd,
                                        HasOperatingPhysician = hasOperatingPhysician
                                    });
                                }
                                catch (Exception rowEx)
                                {
                                    _logger.LogWarning(rowEx, "Error reading denomination row, skipping. Error: {Error}", rowEx.Message);
                                    // Continue to next row
                                }
                            }
                        }
                    }
                }

                _logger.LogInformation("Retrieved {Count} denomination search results", results.Count);
                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching denominations with advanced query");
                return StatusCode(500, new { message = "An error occurred while searching denominations", error = ex.Message });
            }
        }

        [HttpGet("Search")]
        public async Task<ActionResult<IEnumerable<HospitalDenomination>>> SearchDenominations([FromQuery] string query, [FromQuery] string? costCenterFilter = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return await GetDenominations();
                }

                // Query from database with search filter - prioritize SmallDescription
                // Use raw SQL with COALESCE to handle NULL values properly
                var searchPattern = $"%{query}%";
                _logger.LogInformation("🔍 Searching denominations with pattern: {Pattern}, CostCenter filter: {CostCenterFilter}", searchPattern, costCenterFilter);
                
                // Build CostCenter filter condition
                var costCenterCondition = "";
                var sqlParams = new List<object> { searchPattern, searchPattern };
                
                if (!string.IsNullOrWhiteSpace(costCenterFilter))
                {
                    // CostCenter is stored as string in the database (could be '5', '11', etc.)
                    // The filter will be like: "5,11,12" or "1" or "2,6,7"
                    var costCenters = costCenterFilter.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    if (costCenters.Length > 0)
                    {
                        // Build IN clause: CAST([CostCenter] AS INT) IN (5,11,12)
                        // But since CostCenter is string, we need to handle conversion safely
                        var costCenterValues = string.Join(",", costCenters.Select(cc => cc.Trim()));
                        costCenterCondition = $" AND (CAST([CostCenter] AS INT) IN ({costCenterValues}))";
                        _logger.LogInformation("📍 Applying CostCenter filter: {Filter}", costCenterCondition);
                    }
                }
                
                var sqlQuery = $@"
                    SELECT 
                        [ID] AS [Id],
                        COALESCE([SmallDescription], '') AS [SmallDescription],
                        COALESCE([LongDescription], '') AS [LongDescription],
                        COALESCE([Code], '') AS [Code],
                        COALESCE([Abreviation], '') AS [Abreviation],
                        COALESCE([HasOperatingPhysician], 0) AS [HasOperatingPhysician],
                        COALESCE([HasAnesthesiaPhysician], 0) AS [HasAnesthesiaPhysician],
                        COALESCE([HasOperatingRoom], 0) AS [HasOperatingRoom],
                        COALESCE([IsHonoraryExcluded], 0) AS [IsHonoraryExcluded],
                        COALESCE([IsResidenceRelated], 0) AS [IsResidenceRelated],
                        COALESCE([HasMedicalResult], 0) AS [HasMedicalResult],
                        COALESCE([App], 0) AS [App],
                        COALESCE(CAST([OperatingRoom] AS NVARCHAR(MAX)), '') AS [OperatingRoom],
                        COALESCE([CoefficientCode], '') AS [CoefficientCode],
                        COALESCE([CoefficientValue], 0) AS [CoefficientValue],
                        COALESCE([CashPriceUsd], 0) AS [CashPriceUsd],
                        COALESCE([CashPriceLlbp], 0) AS [CashPriceLlbp],
                        COALESCE([Status], 0) AS [Status],
                        COALESCE(CAST([DisplayOrder] AS NVARCHAR(MAX)), '') AS [DisplayOrder],
                        COALESCE(CAST([CostCenter] AS NVARCHAR(MAX)), '') AS [CostCenter],
                        COALESCE([ExpectedResidenceDays], 0) AS [ExpectedResidenceDays],
                        COALESCE([IsSubItem], 0) AS [IsSubItem],
                        COALESCE([IsDeleted], 0) AS [IsDeleted],
                        COALESCE([CreatedBy], 0) AS [CreatedBy],
                        COALESCE([ModifiedBy], 0) AS [ModifiedBy],
                        COALESCE([CreatedDate], GETDATE()) AS [CreatedDate],
                        NULL AS [ModifiedDate],
                        COALESCE([StartDate], 0) AS [StartDate],
                        COALESCE([StartDateLabel], 0) AS [StartDateLabel],
                        COALESCE([EndDate], 0) AS [EndDate],
                        COALESCE([EndDateLabel], 0) AS [EndDateLabel],
                        COALESCE([IsSelectedOrNot], 0) AS [IsSelectedOrNot],
                        COALESCE([SeverityID], 0) AS [SeverityId],
                        COALESCE([StatusID], 0) AS [StatusId],
                        COALESCE([Comments], '') AS [Comments],
                        COALESCE([InCrAppCode], '') AS [InCrAppCode],
                        COALESCE([InCaAppCode], '') AS [InCaAppCode],
                        COALESCE([OutCrAppCode], '') AS [OutCrAppCode],
                        COALESCE([OutCaAppCode], '') AS [OutCaAppCode],
                        COALESCE([DenominationDefaultTime], 0) AS [DenominationDefaultTime],
                        COALESCE([Rate], 0) AS [Rate],
                        COALESCE([HasVideo], 0) AS [HasVideo],
                        COALESCE([IsOpenHeart], 0) AS [IsOpenHeart],
                        COALESCE([IsReferralShare], 0) AS [IsReferralShare],
                        COALESCE([ReferralAmount], 0) AS [ReferralAmount],
                        COALESCE([DenominationGroupID], 0) AS [DenominationGroupId],
                        COALESCE([IsClassRelated], 0) AS [IsClassRelated],
                        COALESCE([CreditDiscount], '') AS [CreditDiscount],
                        COALESCE([CashDiscount], '') AS [CashDiscount],
                        COALESCE([IsPrintable], 0) AS [IsPrintable]
                    FROM [dbo].[Denomination]
                    WHERE COALESCE([IsDeleted], 0) = 0
                        AND [SmallDescription] IS NOT NULL
                        AND [Code] IS NOT NULL
                        AND (LOWER([SmallDescription]) LIKE LOWER({0})
                            OR LOWER([Code]) LIKE LOWER({0})
                            OR ([LongDescription] IS NOT NULL AND LOWER([LongDescription]) LIKE LOWER({0})))
                        {costCenterCondition}
                    ORDER BY 
                        CASE WHEN LOWER([SmallDescription]) LIKE LOWER({0}) THEN 1 ELSE 2 END,
                        [SmallDescription],
                        [Code]";
                
                _logger.LogInformation("📝 Executing SQL query with pattern: {Pattern}", searchPattern);
                
                var denominations = await _context.Database
                    .SqlQueryRaw<HospitalDenomination>(sqlQuery, sqlParams.ToArray())
                    .ToListAsync();
                
                _logger.LogInformation("✅ Successfully retrieved {Count} denominations", denominations.Count);

                _logger.LogInformation("Found {Count} denominations matching query: {Query}", denominations.Count, query);
                return Ok(denominations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching denominations with query: {Query}. Stack trace: {StackTrace}", query, ex.StackTrace);
                return StatusCode(500, $"Internal server error: {ex.Message}. Details: {ex.InnerException?.Message}");
            }
        }

        [HttpGet("QuickAdmission")]
        public async Task<ActionResult<IEnumerable<HospitalDenomination>>> GetDenominationsForQuickAdmission()
        {
            try
            {
                // Query actual denominations from HospitalDefinition.dbo.Denomination
                // Use raw SQL with COALESCE to handle NULL values properly
                var sqlQuery = @"
                    SELECT 
                        [ID] AS [Id],
                        COALESCE([SmallDescription], '') AS [SmallDescription],
                        COALESCE([LongDescription], '') AS [LongDescription],
                        COALESCE([Code], '') AS [Code],
                        COALESCE([Abreviation], '') AS [Abreviation],
                        COALESCE([HasOperatingPhysician], 0) AS [HasOperatingPhysician],
                        COALESCE([HasAnesthesiaPhysician], 0) AS [HasAnesthesiaPhysician],
                        COALESCE([HasOperatingRoom], 0) AS [HasOperatingRoom],
                        COALESCE([IsHonoraryExcluded], 0) AS [IsHonoraryExcluded],
                        COALESCE([IsResidenceRelated], 0) AS [IsResidenceRelated],
                        COALESCE([HasMedicalResult], 0) AS [HasMedicalResult],
                        COALESCE([App], 0) AS [App],
                        COALESCE(CAST([OperatingRoom] AS NVARCHAR(MAX)), '') AS [OperatingRoom],
                        COALESCE([CoefficientCode], '') AS [CoefficientCode],
                        COALESCE([CoefficientValue], 0) AS [CoefficientValue],
                        COALESCE([CashPriceUsd], 0) AS [CashPriceUsd],
                        COALESCE([CashPriceLlbp], 0) AS [CashPriceLlbp],
                        COALESCE([Status], 0) AS [Status],
                        COALESCE(CAST([DisplayOrder] AS NVARCHAR(MAX)), '') AS [DisplayOrder],
                        COALESCE(CAST([CostCenter] AS NVARCHAR(MAX)), '') AS [CostCenter],
                        COALESCE([ExpectedResidenceDays], 0) AS [ExpectedResidenceDays],
                        COALESCE([IsSubItem], 0) AS [IsSubItem],
                        COALESCE([IsDeleted], 0) AS [IsDeleted],
                        COALESCE([CreatedBy], 0) AS [CreatedBy],
                        COALESCE([ModifiedBy], 0) AS [ModifiedBy],
                        COALESCE([CreatedDate], GETDATE()) AS [CreatedDate],
                        NULL AS [ModifiedDate],
                        COALESCE([StartDate], 0) AS [StartDate],
                        COALESCE([StartDateLabel], 0) AS [StartDateLabel],
                        COALESCE([EndDate], 0) AS [EndDate],
                        COALESCE([EndDateLabel], 0) AS [EndDateLabel],
                        COALESCE([IsSelectedOrNot], 0) AS [IsSelectedOrNot],
                        COALESCE([SeverityID], 0) AS [SeverityId],
                        COALESCE([StatusID], 0) AS [StatusId],
                        COALESCE([Comments], '') AS [Comments],
                        COALESCE([InCrAppCode], '') AS [InCrAppCode],
                        COALESCE([InCaAppCode], '') AS [InCaAppCode],
                        COALESCE([OutCrAppCode], '') AS [OutCrAppCode],
                        COALESCE([OutCaAppCode], '') AS [OutCaAppCode],
                        COALESCE([DenominationDefaultTime], 0) AS [DenominationDefaultTime],
                        COALESCE([Rate], 0) AS [Rate],
                        COALESCE([HasVideo], 0) AS [HasVideo],
                        COALESCE([IsOpenHeart], 0) AS [IsOpenHeart],
                        COALESCE([IsReferralShare], 0) AS [IsReferralShare],
                        COALESCE([ReferralAmount], 0) AS [ReferralAmount],
                        COALESCE([DenominationGroupID], 0) AS [DenominationGroupId],
                        COALESCE([IsClassRelated], 0) AS [IsClassRelated],
                        COALESCE([CreditDiscount], '') AS [CreditDiscount],
                        COALESCE([CashDiscount], '') AS [CashDiscount],
                        COALESCE([IsPrintable], 0) AS [IsPrintable]
                    FROM [dbo].[Denomination]
                    WHERE COALESCE([IsDeleted], 0) = 0
                        AND [SmallDescription] IS NOT NULL
                        AND [Code] IS NOT NULL
                    ORDER BY [Code], [SmallDescription]";
                
                _logger.LogInformation("📝 Loading denominations for Quick Admission");
                
                var denominations = await _context.Database
                    .SqlQueryRaw<HospitalDenomination>(sqlQuery)
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} denominations for quick admission", denominations.Count);
                return Ok(denominations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving denominations for quick admission. Stack trace: {StackTrace}", ex.StackTrace);
                return StatusCode(500, $"Internal server error: {ex.Message}. Details: {ex.InnerException?.Message}");
            }
        }

        [HttpPost]
        public async Task<ActionResult<HospitalDenomination>> CreateDenomination(HospitalDenomination denomination)
        {
            try
            {
                // Generate new ID
                var maxId = await _context.Database
                    .SqlQueryRaw<int>("SELECT ISNULL(MAX([ID]), 0) FROM [HospitalDefinition].[dbo].[Denomination] WHERE COALESCE([IsDeleted], 0) = 0")
                    .FirstOrDefaultAsync();

                denomination.Id = maxId + 1;
                denomination.CreatedDate = DateTime.Now;
                denomination.ModifiedDate = DateTime.Now;
                denomination.IsDeleted = 0;

                // Insert using raw SQL
                var sql = @"INSERT INTO [HospitalDefinition].[dbo].[Denomination] 
                    ([ID], [SmallDescription], [LongDescription], [Code], [Abreviation], [HasOperatingPhysician], [HasAnesthesiaPhysician], 
                     [HasOperatingRoom], [IsHonoraryExcluded], [IsResidenceRelated], [HasMedicalResult], [App], [OperatingRoom], 
                     [CoefficientCode], [CoefficientValue], [Status], [DisplayOrder], [CostCenter], [ExpectedResidenceDays], [IsSubItem], 
                     [IsDeleted], [CreatedBy], [ModifiedBy], [CreatedDate], [ModifiedDate], [StartDate], [StartDateLabel], [EndDate], 
                     [EndDateLabel], [IsSelectedOrNot], [SeverityID], [StatusID], [Comments], [InCrAppCode], [InCaAppCode], [OutCrAppCode], 
                     [OutCaAppCode], [DenominationDefaultTime], [Rate], [HasVideo], [IsOpenHeart], [IsReferralShare], [ReferralAmount], 
                     [DenominationGroupID], [IsClassRelated], [CreditDiscount], [CashDiscount], [IsPrintable])
                    VALUES 
                    ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13}, {14}, {15}, {16}, {17}, {18}, {19}, {20}, {21}, {22}, {23}, {24}, {25}, {26}, {27}, {28}, {29}, {30}, {31}, {32}, {33}, {34}, {35}, {36}, {37}, {38}, {39}, {40}, {41}, {42}, {43}, {44}, {45}, {46}, {47}, {48}, {49}, {50})";

                await _context.Database.ExecuteSqlRawAsync(sql,
                    denomination.Id, denomination.SmallDescription, denomination.LongDescription, denomination.Code, denomination.Abreviation,
                    denomination.HasOperatingPhysician, denomination.HasAnesthesiaPhysician, denomination.HasOperatingRoom, denomination.IsHonoraryExcluded,
                    denomination.IsResidenceRelated, denomination.HasMedicalResult, denomination.App, denomination.OperatingRoom, denomination.CoefficientCode,
                    denomination.CoefficientValue, denomination.Status, denomination.DisplayOrder, denomination.CostCenter, denomination.ExpectedResidenceDays,
                    denomination.IsSubItem, denomination.IsDeleted, denomination.CreatedBy, denomination.ModifiedBy, denomination.CreatedDate,
                    denomination.ModifiedDate, denomination.StartDate, denomination.StartDateLabel, denomination.EndDate, denomination.EndDateLabel,
                    denomination.IsSelectedOrNot, denomination.SeverityId, denomination.StatusId, denomination.Comments, denomination.InCrAppCode,
                    denomination.InCaAppCode, denomination.OutCrAppCode, denomination.OutCaAppCode, denomination.DenominationDefaultTime,
                    denomination.Rate, denomination.HasVideo, denomination.IsOpenHeart, denomination.IsReferralShare, denomination.ReferralAmount,
                    denomination.DenominationGroupId, denomination.IsClassRelated, denomination.CreditDiscount, denomination.CashDiscount,
                    denomination.IsPrintable);

                return CreatedAtAction(nameof(GetDenomination), new { id = denomination.Id }, denomination);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating denomination");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDenomination(int id, HospitalDenomination denomination)
        {
            if (id != denomination.Id)
            {
                return BadRequest();
            }

            try
            {
                denomination.ModifiedDate = DateTime.Now;

                var sql = @"UPDATE [HospitalDefinition].[dbo].[Denomination] SET 
                    [SmallDescription] = {1}, [LongDescription] = {2}, [Code] = {3}, [Abreviation] = {4}, [HasOperatingPhysician] = {5}, 
                    [HasAnesthesiaPhysician] = {6}, [HasOperatingRoom] = {7}, [IsHonoraryExcluded] = {8}, [IsResidenceRelated] = {9}, 
                    [HasMedicalResult] = {10}, [App] = {11}, [OperatingRoom] = {12}, [CoefficientCode] = {13}, [CoefficientValue] = {14}, 
                    [Status] = {15}, [DisplayOrder] = {16}, [CostCenter] = {17}, [ExpectedResidenceDays] = {18}, [IsSubItem] = {19}, 
                    [ModifiedBy] = {20}, [ModifiedDate] = {21}, [StartDate] = {22}, [StartDateLabel] = {23}, [EndDate] = {24}, 
                    [EndDateLabel] = {25}, [IsSelectedOrNot] = {26}, [SeverityID] = {27}, [StatusID] = {28}, [Comments] = {29}, 
                    [InCrAppCode] = {30}, [InCaAppCode] = {31}, [OutCrAppCode] = {32}, [OutCaAppCode] = {33}, [DenominationDefaultTime] = {34}, 
                    [Rate] = {35}, [HasVideo] = {36}, [IsOpenHeart] = {37}, [IsReferralShare] = {38}, [ReferralAmount] = {39}, 
                    [DenominationGroupID] = {40}, [IsClassRelated] = {41}, [CreditDiscount] = {42}, [CashDiscount] = {43}, [IsPrintable] = {44}
                    WHERE [ID] = {0}";

                var rowsAffected = await _context.Database.ExecuteSqlRawAsync(sql,
                    id, denomination.SmallDescription, denomination.LongDescription, denomination.Code, denomination.Abreviation,
                    denomination.HasOperatingPhysician, denomination.HasAnesthesiaPhysician, denomination.HasOperatingRoom, denomination.IsHonoraryExcluded,
                    denomination.IsResidenceRelated, denomination.HasMedicalResult, denomination.App, denomination.OperatingRoom, denomination.CoefficientCode,
                    denomination.CoefficientValue, denomination.Status, denomination.DisplayOrder, denomination.CostCenter, denomination.ExpectedResidenceDays,
                    denomination.IsSubItem, denomination.ModifiedBy, denomination.ModifiedDate, denomination.StartDate, denomination.StartDateLabel,
                    denomination.EndDate, denomination.EndDateLabel, denomination.IsSelectedOrNot, denomination.SeverityId, denomination.StatusId,
                    denomination.Comments, denomination.InCrAppCode, denomination.InCaAppCode, denomination.OutCrAppCode, denomination.OutCaAppCode,
                    denomination.DenominationDefaultTime, denomination.Rate, denomination.HasVideo, denomination.IsOpenHeart, denomination.IsReferralShare,
                    denomination.ReferralAmount, denomination.DenominationGroupId, denomination.IsClassRelated, denomination.CreditDiscount,
                    denomination.CashDiscount, denomination.IsPrintable);

                if (rowsAffected == 0)
                {
                    return NotFound();
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating denomination {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDenomination(int id)
        {
            try
            {
                var sql = "UPDATE [HospitalDefinition].[dbo].[Denomination] SET [IsDeleted] = 1, [ModifiedDate] = {1} WHERE [ID] = {0}";
                var rowsAffected = await _context.Database.ExecuteSqlRawAsync(sql, id, DateTime.Now);

                if (rowsAffected == 0)
                {
                    return NotFound();
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting denomination {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
