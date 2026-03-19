using LIS.Api.Data;
using LIS.Api.Models.UserManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LIS.Api.Controllers
{
    [ApiController]
    [Route("api/user-management/profiles")]
    public class UserManagementProfilesController : ControllerBase
    {
        private readonly HISUsersDbContext _db;

        public UserManagementProfilesController(HISUsersDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProfileDefinition>>> GetAll()
        {
            var items = await _db.ProfileDefinitions
                .AsNoTracking()
                .Where(p => !p.IsDeleted)
                .OrderBy(p => p.Name)
                .ToListAsync();

            return Ok(items);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ProfileDefinition>> GetById(int id)
        {
            var item = await _db.ProfileDefinitions
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (item == null)
            {
                return NotFound(new { message = $"ProfileDefinition with ID {id} not found" });
            }

            return Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<ProfileDefinition>> Create([FromBody] ProfileDefinition input)
        {
            input.Id = 0;
            input.CreatedDate = DateTime.UtcNow;
            if (input.CreatedBy == 0)
            {
                input.CreatedBy = -1;
            }
            input.IsDeleted = false;

            _db.ProfileDefinitions.Add(input);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = input.Id }, input);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] ProfileDefinition input)
        {
            if (id != input.Id)
            {
                return BadRequest(new { message = "ID mismatch" });
            }

            var existing = await _db.ProfileDefinitions.FirstOrDefaultAsync(p => p.Id == id);
            if (existing == null)
            {
                return NotFound(new { message = $"ProfileDefinition with ID {id} not found" });
            }

            existing.Name = input.Name;
            existing.IsAdmin = input.IsAdmin;
            existing.IsDeleted = input.IsDeleted;
            existing.ModifiedDate = DateTime.UtcNow;
            existing.ModifiedBy = input.ModifiedBy == 0 ? existing.ModifiedBy ?? -1 : input.ModifiedBy;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _db.ProfileDefinitions.FirstOrDefaultAsync(p => p.Id == id);
            if (existing == null)
            {
                return NotFound(new { message = $"ProfileDefinition with ID {id} not found" });
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
