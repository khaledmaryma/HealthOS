using LIS.Api.Data;
using LIS.Api.Models.UserManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LIS.Api.Controllers
{
    [ApiController]
    [Route("api/user-management/permissions")]
    public class UserManagementPermissionsController : ControllerBase
    {
        private readonly HISUsersDbContext _db;

        public UserManagementPermissionsController(HISUsersDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PermissionDefinition>>> GetAll()
        {
            var query = _db.PermissionDefinitions
                .AsNoTracking()
                .Where(p => !p.IsDeleted);

            var items = await query
                .OrderBy(p => p.Name)
                .ToListAsync();

            return Ok(items);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<PermissionDefinition>> GetById(int id)
        {
            var item = await _db.PermissionDefinitions
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (item == null)
            {
                return NotFound(new { message = $"PermissionDefinition with ID {id} not found" });
            }

            return Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<PermissionDefinition>> Create([FromBody] PermissionDefinition input)
        {
            input.Id = 0;
            input.CreatedDate = DateTime.UtcNow;
            if (input.CreatedBy == 0)
            {
                input.CreatedBy = -1;
            }
            input.IsDeleted = false;

            _db.PermissionDefinitions.Add(input);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = input.Id }, input);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] PermissionDefinition input)
        {
            if (id != input.Id)
            {
                return BadRequest(new { message = "ID mismatch" });
            }

            var existing = await _db.PermissionDefinitions.FirstOrDefaultAsync(p => p.Id == id);
            if (existing == null)
            {
                return NotFound(new { message = $"PermissionDefinition with ID {id} not found" });
            }

            existing.Code = input.Code;
            existing.Name = input.Name;
            existing.Description = input.Description;
            existing.ApplicationId = input.ApplicationId;
            existing.ScreenId = input.ScreenId;
            existing.Action = input.Action;
            existing.PermissionKey = input.PermissionKey;
            existing.IsDeleted = input.IsDeleted;
            existing.ModifiedDate = DateTime.UtcNow;
            existing.ModifiedBy = input.ModifiedBy == 0 ? existing.ModifiedBy ?? -1 : input.ModifiedBy;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _db.PermissionDefinitions.FirstOrDefaultAsync(p => p.Id == id);
            if (existing == null)
            {
                return NotFound(new { message = $"PermissionDefinition with ID {id} not found" });
            }

            existing.IsDeleted = true;
            existing.ModifiedDate = DateTime.UtcNow;
            if (existing.ModifiedBy == null)
            {
                existing.ModifiedBy = -1;
            }

            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
