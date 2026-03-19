using LIS.Api.Data;
using LIS.Api.Models.UserManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LIS.Api.Controllers
{
    [ApiController]
    [Route("api/user-management/screens")]
    public class UserManagementScreensController : ControllerBase
    {
        private readonly HISUsersDbContext _db;

        public UserManagementScreensController(HISUsersDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ScreenDefinition>>> GetAll([FromQuery] int? appId)
        {
            var query = _db.ScreenDefinitions
                .AsNoTracking()
                .Where(s => !s.IsDeleted);

            if (appId.HasValue)
            {
                query = query.Where(s => s.AppId == appId.Value);
            }

            var items = await query
                .OrderBy(s => s.Name)
                .ToListAsync();

            return Ok(items);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ScreenDefinition>> GetById(int id)
        {
            var item = await _db.ScreenDefinitions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id);

            if (item == null)
            {
                return NotFound(new { message = $"ScreenDefinition with ID {id} not found" });
            }

            return Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<ScreenDefinition>> Create([FromBody] ScreenDefinition input)
        {
            input.Id = 0;
            input.CreatedDate = DateTime.UtcNow;
            if (input.CreatedBy == 0)
            {
                input.CreatedBy = -1;
            }
            input.IsDeleted = false;

            _db.ScreenDefinitions.Add(input);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = input.Id }, input);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] ScreenDefinition input)
        {
            if (id != input.Id)
            {
                return BadRequest(new { message = "ID mismatch" });
            }

            var existing = await _db.ScreenDefinitions.FirstOrDefaultAsync(s => s.Id == id);
            if (existing == null)
            {
                return NotFound(new { message = $"ScreenDefinition with ID {id} not found" });
            }

            existing.AppId = input.AppId;
            existing.Code = input.Code;
            existing.Name = input.Name;
            existing.Route = input.Route;
            existing.IsDeleted = input.IsDeleted;
            existing.ModifiedDate = DateTime.UtcNow;
            existing.ModifiedBy = input.ModifiedBy == 0 ? existing.ModifiedBy ?? -1 : input.ModifiedBy;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _db.ScreenDefinitions.FirstOrDefaultAsync(s => s.Id == id);
            if (existing == null)
            {
                return NotFound(new { message = $"ScreenDefinition with ID {id} not found" });
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
