using LIS.Api.Data;
using LIS.Api.Models.UserManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LIS.Api.Controllers
{
    [ApiController]
    [Route("api/user-management/users")]
    public class UserManagementUsersController : ControllerBase
    {
        private readonly HISUsersDbContext _db;

        public UserManagementUsersController(HISUsersDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDefinition>>> GetAll()
        {
            var items = await _db.UserDefinitions
                .AsNoTracking()
                .Where(u => !u.IsDeleted)
                .OrderBy(u => u.Username)
                .ToListAsync();

            return Ok(items);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<UserDefinition>> GetById(int id)
        {
            var item = await _db.UserDefinitions
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id);

            if (item == null)
            {
                return NotFound(new { message = $"UserDefinition with ID {id} not found" });
            }

            return Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<UserDefinition>> Create([FromBody] UserDefinition input)
        {
            if (input.ProfileId <= 0)
            {
                return BadRequest(new { message = "Profile is required" });
            }
            if (string.IsNullOrWhiteSpace(input.Password))
            {
                return BadRequest(new { message = "Password is required" });
            }

            input.Id = 0;
            input.CreatedDate = DateTime.UtcNow;
            if (input.CreatedBy == 0)
            {
                input.CreatedBy = -1;
            }
            input.IsDeleted = false;

            _db.UserDefinitions.Add(input);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = input.Id }, input);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UserDefinition input)
        {
            if (id != input.Id)
            {
                return BadRequest(new { message = "ID mismatch" });
            }

            var existing = await _db.UserDefinitions.FirstOrDefaultAsync(u => u.Id == id);
            if (existing == null)
            {
                return NotFound(new { message = $"UserDefinition with ID {id} not found" });
            }

            existing.Username = input.Username;
            existing.ProfileId = input.ProfileId;
            existing.FullName = input.FullName;
            existing.Email = input.Email;
            existing.IsActive = input.IsActive;
            existing.IsDeleted = input.IsDeleted;
            if (!string.IsNullOrWhiteSpace(input.Password))
            {
                existing.Password = input.Password;
            }
            existing.ModifiedDate = DateTime.UtcNow;
            existing.ModifiedBy = input.ModifiedBy == 0 ? existing.ModifiedBy ?? -1 : input.ModifiedBy;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _db.UserDefinitions.FirstOrDefaultAsync(u => u.Id == id);
            if (existing == null)
            {
                return NotFound(new { message = $"UserDefinition with ID {id} not found" });
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

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { message = "Username and password are required" });
            }

            var user = await _db.UserDefinitions
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Username == request.Username && !u.IsDeleted && u.IsActive);

            if (user == null || !string.Equals(user.Password, request.Password, StringComparison.Ordinal))
            {
                return Unauthorized(new { message = "Invalid credentials" });
            }

            var profile = await _db.ProfileDefinitions
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == user.ProfileId && !p.IsDeleted);
            var isAdminProfile = profile?.IsAdmin == true;

            if (isAdminProfile)
            {
                var adminApps = await _db.AppDefinitions
                    .AsNoTracking()
                    .Where(a => !a.IsDeleted)
                    .OrderBy(a => a.Name)
                    .ToListAsync();

                var adminScreens = await _db.ScreenDefinitions
                    .AsNoTracking()
                    .Where(s => !s.IsDeleted)
                    .OrderBy(s => s.Name)
                    .ToListAsync();

                var adminPermissions = await _db.PermissionDefinitions
                    .AsNoTracking()
                    .Where(p => !p.IsDeleted)
                    .OrderBy(p => p.Name)
                    .ToListAsync();

                var adminAccess = new LoginAccess(
                    adminApps.Select(app => new LoginAccessApp(app.Id, app.Code, app.Name, true)).ToList(),
                    adminScreens.Select(screen => new LoginAccessScreen(screen.Id, screen.AppId, screen.Code, screen.Name, screen.Route, true)).ToList(),
                    adminPermissions.Select(permission => new LoginAccessPermission(
                        permission.Id,
                        permission.ApplicationId,
                        permission.ScreenId,
                        permission.Code,
                        permission.Name,
                        true,
                        true,
                        true,
                        true,
                        true,
                        true
                    )).ToList()
                );

                return Ok(new LoginResponse(user.Id, user.Username, user.FullName, adminAccess));
            }

            var permissionRows = await _db.ProfilePermissions
                .AsNoTracking()
                .Where(pp => pp.ProfileId == user.ProfileId && !pp.IsDeleted)
                .Join(
                    _db.PermissionDefinitions.AsNoTracking().Where(p => !p.IsDeleted),
                    pp => pp.PermissionId,
                    p => p.Id,
                    (pp, p) => new { ProfilePermission = pp, Permission = p }
                )
                .ToListAsync();

            var appIds = permissionRows
                .Select(row => row.Permission.ApplicationId)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToList();

            var screenIds = permissionRows
                .Select(row => row.Permission.ScreenId)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToList();

            var appIdSet = appIds.ToHashSet();
            var screenIdSet = screenIds.ToHashSet();

            var apps = await _db.AppDefinitions
                .AsNoTracking()
                .Where(a => !a.IsDeleted)
                .ToListAsync();
            apps = apps.Where(a => appIdSet.Contains(a.Id)).ToList();

            var screens = await _db.ScreenDefinitions
                .AsNoTracking()
                .Where(s => !s.IsDeleted)
                .ToListAsync();
            screens = screens.Where(s => screenIdSet.Contains(s.Id)).ToList();

            var appLookup = apps.ToDictionary(app => app.Id);
            var screenLookup = screens.ToDictionary(screen => screen.Id);

            var accessApps = appIds
                .Select(appId =>
                {
                    appLookup.TryGetValue(appId, out var app);
                    var hasAccess = permissionRows.Any(row =>
                        row.Permission.ApplicationId == appId && row.ProfilePermission.HasAccessToApp);
                    return new LoginAccessApp(
                        appId,
                        app?.Code ?? appId.ToString(),
                        app?.Name ?? app?.Code ?? appId.ToString(),
                        hasAccess
                    );
                })
                .OrderBy(app => app.Name)
                .ToList();

            var accessScreens = screenIds
                .Select(screenId =>
                {
                    screenLookup.TryGetValue(screenId, out var screen);
                    var hasAccess = permissionRows.Any(row =>
                        row.Permission.ScreenId == screenId && row.ProfilePermission.HasAccessToMenu);
                    return new LoginAccessScreen(
                        screenId,
                        screen?.AppId ?? 0,
                        screen?.Code ?? screenId.ToString(),
                        screen?.Name ?? screen?.Code ?? screenId.ToString(),
                        screen?.Route,
                        hasAccess
                    );
                })
                .OrderBy(screen => screen.Name)
                .ToList();

            var accessPermissions = permissionRows
                .Select(row => new LoginAccessPermission(
                    row.Permission.Id,
                    row.Permission.ApplicationId,
                    row.Permission.ScreenId,
                    row.Permission.Code,
                    row.Permission.Name,
                    row.ProfilePermission.CanSee,
                    row.ProfilePermission.CanAdd,
                    row.ProfilePermission.CanModify,
                    row.ProfilePermission.CanDelete,
                    row.ProfilePermission.HasAccessToMenu,
                    row.ProfilePermission.HasAccessToApp
                ))
                .OrderBy(permission => permission.Name)
                .ToList();

            var access = new LoginAccess(accessApps, accessScreens, accessPermissions);

            return Ok(new LoginResponse(user.Id, user.Username, user.FullName, access));
        }

        public sealed record LoginRequest(string Username, string Password);
        public sealed record LoginAccessApp(int Id, string Code, string Name, bool HasAccessToApp);
        public sealed record LoginAccessScreen(int Id, int AppId, string Code, string Name, string? Route, bool HasAccessToMenu);
        public sealed record LoginAccessPermission(
            int Id,
            int? ApplicationId,
            int? ScreenId,
            string Code,
            string Name,
            bool CanSee,
            bool CanAdd,
            bool CanModify,
            bool CanDelete,
            bool HasAccessToMenu,
            bool HasAccessToApp
        );
        public sealed record LoginAccess(
            IReadOnlyList<LoginAccessApp> Applications,
            IReadOnlyList<LoginAccessScreen> Screens,
            IReadOnlyList<LoginAccessPermission> Permissions
        );
        public sealed record LoginResponse(int Id, string Username, string FullName, LoginAccess Access);
    }
}
