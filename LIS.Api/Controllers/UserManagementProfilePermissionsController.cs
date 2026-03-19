using LIS.Api.Data;
using LIS.Api.Models.UserManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LIS.Api.Controllers
{
    [ApiController]
    [Route("api/user-management/profile-permissions")]
    public class UserManagementProfilePermissionsController : ControllerBase
    {
        private readonly HISUsersDbContext _db;

        public UserManagementProfilePermissionsController(HISUsersDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProfilePermission>>> GetAll([FromQuery] int? profileId)
        {
            var query = _db.ProfilePermissions
                .AsNoTracking()
                .Where(pp => !pp.IsDeleted);

            if (profileId.HasValue)
            {
                query = query.Where(pp => pp.ProfileId == profileId.Value);
            }

            var items = await query
                .OrderBy(pp => pp.ProfileId)
                .ThenBy(pp => pp.PermissionId)
                .ToListAsync();

            return Ok(items);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ProfilePermission>> GetById(int id)
        {
            var item = await _db.ProfilePermissions
                .AsNoTracking()
                .FirstOrDefaultAsync(pp => pp.Id == id);

            if (item == null)
            {
                return NotFound(new { message = $"ProfilePermission with ID {id} not found" });
            }

            return Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<ProfilePermission>> Create([FromBody] ProfilePermission input)
        {
            input.Id = 0;
            input.CreatedDate = DateTime.UtcNow;
            if (input.CreatedBy == 0)
            {
                input.CreatedBy = -1;
            }
            input.IsDeleted = false;

            _db.ProfilePermissions.Add(input);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = input.Id }, input);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] ProfilePermission input)
        {
            if (id != input.Id)
            {
                return BadRequest(new { message = "ID mismatch" });
            }

            var existing = await _db.ProfilePermissions.FirstOrDefaultAsync(pp => pp.Id == id);
            if (existing == null)
            {
                return NotFound(new { message = $"ProfilePermission with ID {id} not found" });
            }

            existing.ProfileId = input.ProfileId;
            existing.PermissionId = input.PermissionId;
            existing.CanAdd = input.CanAdd;
            existing.CanModify = input.CanModify;
            existing.CanDelete = input.CanDelete;
            existing.CanSee = input.CanSee;
            existing.HasAccessToMenu = input.HasAccessToMenu;
            existing.HasAccessToApp = input.HasAccessToApp;
            existing.IsDeleted = input.IsDeleted;
            existing.ModifiedDate = DateTime.UtcNow;
            existing.ModifiedBy = input.ModifiedBy == 0 ? existing.ModifiedBy ?? -1 : input.ModifiedBy;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _db.ProfilePermissions.FirstOrDefaultAsync(pp => pp.Id == id);
            if (existing == null)
            {
                return NotFound(new { message = $"ProfilePermission with ID {id} not found" });
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
