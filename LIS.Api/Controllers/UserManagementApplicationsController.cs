using LIS.Api.Data;
using LIS.Api.Models.UserManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LIS.Api.Controllers
{
    [ApiController]
    [Route("api/user-management/applications")]
    public class UserManagementApplicationsController : ControllerBase
    {
        private readonly HISUsersDbContext _db;

        public UserManagementApplicationsController(HISUsersDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<AppDefinition>>> GetAll()
        {
            var items = await _db.AppDefinitions
                .AsNoTracking()
                .Where(a => !a.IsDeleted)
                .OrderBy(a => a.Name)
                .ToListAsync();

            return Ok(items);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<AppDefinition>> GetById(int id)
        {
            var item = await _db.AppDefinitions
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == id);

            if (item == null)
            {
                return NotFound(new { message = $"AppDefinition with ID {id} not found" });
            }

            return Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<AppDefinition>> Create([FromBody] AppDefinition input)
        {
            input.Id = 0;
            input.CreatedDate = DateTime.UtcNow;
            if (input.CreatedBy == 0)
            {
                input.CreatedBy = -1;
            }
            input.IsDeleted = false;

            _db.AppDefinitions.Add(input);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = input.Id }, input);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] AppDefinition input)
        {
            if (id != input.Id)
            {
                return BadRequest(new { message = "ID mismatch" });
            }

            var existing = await _db.AppDefinitions.FirstOrDefaultAsync(a => a.Id == id);
            if (existing == null)
            {
                return NotFound(new { message = $"AppDefinition with ID {id} not found" });
            }

            existing.Code = input.Code;
            existing.Name = input.Name;
            existing.IsDeleted = input.IsDeleted;
            existing.ModifiedDate = DateTime.UtcNow;
            existing.ModifiedBy = input.ModifiedBy == 0 ? existing.ModifiedBy ?? -1 : input.ModifiedBy;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _db.AppDefinitions.FirstOrDefaultAsync(a => a.Id == id);
            if (existing == null)
            {
                return NotFound(new { message = $"AppDefinition with ID {id} not found" });
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
