using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LIS.Api.Data;
using LIS.Api.Models;
using Microsoft.Data.SqlClient;

namespace LIS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DepartmentController : ControllerBase
    {
        private readonly HospitalDefinitionDbContext _context;
        private readonly ILogger<DepartmentController> _logger;
        private readonly IConfiguration _configuration;

        public DepartmentController(
            HospitalDefinitionDbContext context, 
            ILogger<DepartmentController> logger,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Department>>> GetDepartments()
        {
            try
            {
                _logger.LogInformation("Starting to retrieve departments from HospitalDefinition database");

                // Try using EF Core first
                try
                {
                    _logger.LogInformation("Attempting EF Core query for departments");
                    var departments = await _context.Departments
                        .Where(d => d.IsDeleted == null || d.IsDeleted == false)
                        .OrderBy(d => d.DepartmentName)
                        .ToListAsync();
                    
                    _logger.LogInformation("EF Core query returned {Count} departments", departments.Count);
                    
                    if (departments.Any())
                    {
                        return Ok(departments);
                    }
                    else
                    {
                        _logger.LogWarning("EF Core query returned 0 departments, trying raw SQL");
                    }
                }
                catch (Exception efEx)
                {
                    _logger.LogWarning(efEx, "EF Core query failed: {Message}, trying raw SQL", efEx.Message);
                }

                // Fallback to raw SQL if EF Core fails (table structure might be different)
                var connectionString = _configuration.GetConnectionString("HospitalDefinitionConnection");
                _logger.LogInformation("Using connection string: {ConnectionString}", connectionString?.Replace("Password=.*;", "Password=***;"));
                
                if (string.IsNullOrEmpty(connectionString))
                {
                    _logger.LogError("HospitalDefinitionConnection string is null or empty");
                    return StatusCode(500, new { message = "Database connection string not configured" });
                }

                var departmentsList = new List<Department>();

                using (var connection = new SqlConnection(connectionString))
                {
                    try
                    {
                        await connection.OpenAsync();
                        _logger.LogInformation("Database connection opened successfully");

                        // First, check if table exists and get its structure
                        var tableCheckQuery = @"
                            SELECT COUNT(*) 
                            FROM INFORMATION_SCHEMA.TABLES 
                            WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'Departments'";
                        
                        using (var checkCmd = new SqlCommand(tableCheckQuery, connection))
                        {
                            var tableExists = (int)await checkCmd.ExecuteScalarAsync();
                            _logger.LogInformation("Table 'Departments' exists: {Exists}", tableExists > 0);
                            
                            if (tableExists == 0)
                            {
                                return StatusCode(500, new { message = "Departments table does not exist in HospitalDefinition database" });
                            }
                        }

                        // Get column names to see what's actually in the table
                        var columnQuery = @"
                            SELECT COLUMN_NAME, DATA_TYPE 
                            FROM INFORMATION_SCHEMA.COLUMNS 
                            WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'Departments'
                            ORDER BY ORDINAL_POSITION";
                        
                        var actualColumns = new Dictionary<string, string>();
                        using (var colCmd = new SqlCommand(columnQuery, connection))
                        {
                            using (var colReader = await colCmd.ExecuteReaderAsync())
                            {
                                while (await colReader.ReadAsync())
                                {
                                    actualColumns[colReader.GetString(0)] = colReader.GetString(1);
                                }
                                _logger.LogInformation("Table columns: {Columns}", string.Join(", ", actualColumns.Keys));
                            }
                        }

                        // Try a simple query first to see if there's any data
                        var countQuery = "SELECT COUNT(*) FROM HospitalDefinition.dbo.Departments";
                        using (var countCmd = new SqlCommand(countQuery, connection))
                        {
                            var totalCount = (int)await countCmd.ExecuteScalarAsync();
                            _logger.LogInformation("Total rows in Departments table (including deleted): {Count}", totalCount);
                        }

                        // Build query dynamically based on actual columns
                        var selectColumns = new List<string>();
                        if (actualColumns.ContainsKey("ID")) selectColumns.Add("ID");
                        if (actualColumns.ContainsKey("Name")) selectColumns.Add("Name");
                        else if (actualColumns.ContainsKey("DepartmentName")) selectColumns.Add("DepartmentName AS Name");
                        if (actualColumns.ContainsKey("Description")) selectColumns.Add("Description");
                        if (actualColumns.ContainsKey("Code")) selectColumns.Add("Code");
                        if (actualColumns.ContainsKey("IsActive")) selectColumns.Add("IsActive");
                        if (actualColumns.ContainsKey("IsDeleted")) selectColumns.Add("IsDeleted");
                        if (actualColumns.ContainsKey("CreatedBy")) selectColumns.Add("CreatedBy");
                        if (actualColumns.ContainsKey("ModifiedBy")) selectColumns.Add("ModifiedBy");
                        if (actualColumns.ContainsKey("CreatedDate")) selectColumns.Add("CreatedDate");
                        if (actualColumns.ContainsKey("ModifiedDate")) selectColumns.Add("ModifiedDate");

                        if (selectColumns.Count == 0)
                        {
                            _logger.LogError("No recognized columns found in Departments table");
                            return StatusCode(500, new { message = "Departments table structure not recognized" });
                        }

                        // Build WHERE clause for IsDeleted
                        var whereClause = "1=1";
                        if (actualColumns.ContainsKey("IsDeleted"))
                        {
                            whereClause = "IsDeleted = 0 OR IsDeleted IS NULL";
                        }

                        // Build ORDER BY - try Name first, then ID
                        var orderBy = "ID";
                        if (actualColumns.ContainsKey("Name"))
                        {
                            orderBy = "Name";
                        }
                        else if (actualColumns.ContainsKey("DepartmentName"))
                        {
                            orderBy = "DepartmentName";
                        }

                        var query = $@"
                            SELECT {string.Join(", ", selectColumns)}
                            FROM HospitalDefinition.dbo.Departments
                            WHERE {whereClause}
                            ORDER BY {orderBy}";

                        _logger.LogInformation("Executing query: {Query}", query);

                        using (var command = new SqlCommand(query, connection))
                        {
                            using (var reader = await command.ExecuteReaderAsync())
                            {
                                var rowCount = 0;
                                while (await reader.ReadAsync())
                                {
                                    rowCount++;
                                    try
                                    {
                                        var dept = new Department();
                                        
                                        // Get available column names from reader
                                        var availableColumns = new HashSet<string>();
                                        for (int i = 0; i < reader.FieldCount; i++)
                                        {
                                            availableColumns.Add(reader.GetName(i));
                                        }

                                        // Read ID (required)
                                        if (availableColumns.Contains("ID"))
                                        {
                                            dept.Id = reader.IsDBNull(reader.GetOrdinal("ID")) ? 0 : reader.GetInt32(reader.GetOrdinal("ID"));
                                        }

                                        // Try to read Name (could be Name or DepartmentName)
                                        if (availableColumns.Contains("Name"))
                                        {
                                            dept.DepartmentName = reader.IsDBNull(reader.GetOrdinal("Name")) ? null : reader.GetString(reader.GetOrdinal("Name"));
                                        }
                                        else if (availableColumns.Contains("DepartmentName"))
                                        {
                                            dept.DepartmentName = reader.IsDBNull(reader.GetOrdinal("DepartmentName")) ? null : reader.GetString(reader.GetOrdinal("DepartmentName"));
                                        }

                                        if (availableColumns.Contains("Code"))
                                        {
                                            dept.Code = reader.IsDBNull(reader.GetOrdinal("Code")) ? null : reader.GetString(reader.GetOrdinal("Code"));
                                        }
                                        if (availableColumns.Contains("IsActive"))
                                        {
                                            dept.IsActive = reader.IsDBNull(reader.GetOrdinal("IsActive")) ? null : reader.GetBoolean(reader.GetOrdinal("IsActive"));
                                        }
                                        if (availableColumns.Contains("IsDeleted"))
                                        {
                                            dept.IsDeleted = reader.IsDBNull(reader.GetOrdinal("IsDeleted")) ? null : reader.GetBoolean(reader.GetOrdinal("IsDeleted"));
                                        }
                                        if (availableColumns.Contains("CreatedBy"))
                                        {
                                            dept.CreatedBy = reader.IsDBNull(reader.GetOrdinal("CreatedBy")) ? null : reader.GetInt32(reader.GetOrdinal("CreatedBy"));
                                        }
                                        if (availableColumns.Contains("ModifiedBy"))
                                        {
                                            dept.ModifiedBy = reader.IsDBNull(reader.GetOrdinal("ModifiedBy")) ? null : reader.GetInt32(reader.GetOrdinal("ModifiedBy"));
                                        }
                                        if (availableColumns.Contains("CreatedDate"))
                                        {
                                            dept.CreatedDate = reader.IsDBNull(reader.GetOrdinal("CreatedDate")) ? null : reader.GetDateTime(reader.GetOrdinal("CreatedDate"));
                                        }
                                        if (availableColumns.Contains("ModifiedDate"))
                                        {
                                            dept.ModifiedDate = reader.IsDBNull(reader.GetOrdinal("ModifiedDate")) ? null : reader.GetDateTime(reader.GetOrdinal("ModifiedDate"));
                                        }

                                        _logger.LogInformation("Read department: ID={Id}, Name={Name}, Code={Code}", dept.Id, dept.DepartmentName, dept.Code);
                                        departmentsList.Add(dept);
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogError(ex, "Error reading department row {RowCount}, skipping", rowCount);
                                    }
                                }
                                _logger.LogInformation("Read {RowCount} rows from query result", rowCount);
                            }
                        }
                    }
                    catch (Exception sqlEx)
                    {
                        _logger.LogError(sqlEx, "SQL query execution failed: {Message}", sqlEx.Message);
                        return StatusCode(500, new { message = "Error executing SQL query", error = sqlEx.Message, details = sqlEx.ToString() });
                    }
                }

                _logger.LogInformation("Retrieved {Count} departments from HospitalDefinition database", departmentsList.Count);
                
                if (departmentsList.Count == 0)
                {
                    _logger.LogWarning("No departments found in database. This could mean: 1) Table is empty, 2) All records are deleted (IsDeleted=1), 3) Column names don't match");
                }
                
                return Ok(departmentsList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving departments from HospitalDefinition database: {Message}", ex.Message);
                return StatusCode(500, new { message = "An error occurred while retrieving departments", error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Department>> GetDepartment(int id)
        {
            try
            {
                var department = await _context.Departments
                    .FirstOrDefaultAsync(d => d.Id == id && (d.IsDeleted == null || d.IsDeleted == false));

                if (department == null)
                {
                    return NotFound(new { message = $"Department with ID {id} not found" });
                }

                return Ok(department);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving department {Id}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving the department", error = ex.Message });
            }
        }
    }
}

