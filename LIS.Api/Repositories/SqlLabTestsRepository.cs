using LIS.Api.Data;
using LIS.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LIS.Api.Repositories
{
    public class SqlLabTestsRepository : ILabTestsRepository
    {
        private readonly LISDbContext _context;

        public SqlLabTestsRepository(LISDbContext context)
        {
            _context = context;
        }

        public async Task<List<LabTest>> GetAllAsync()
        {
            return await _context.LabTests
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.Priority)
                .ToListAsync();
        }

        public async Task<LabTest?> GetByIdAsync(int id)
        {
            return await _context.LabTests
                .FirstOrDefaultAsync(x => x.ID == id && !x.IsDeleted);
        }

        public async Task<LabTest> CreateAsync(LabTest item)
        {
            item.CreatedDate = DateTime.UtcNow;
            item.IsDeleted = false;
            
            _context.LabTests.Add(item);
            await _context.SaveChangesAsync();
            
            return item;
        }

        public async Task<bool> UpdateAsync(LabTest item)
        {
            var existing = await _context.LabTests
                .FirstOrDefaultAsync(x => x.ID == item.ID && !x.IsDeleted);
            
            if (existing == null)
                return false;

            // Update properties based on LabTest model
            existing.Code = item.Code;
            existing.TestDesciption = item.TestDesciption;
            existing.Denomination = item.Denomination;
            existing.DefaultTextResult = item.DefaultTextResult;
            existing.DisplayOrder = item.DisplayOrder;
            existing.IsACollection = item.IsACollection;
            existing.HasReferenceRange = item.HasReferenceRange;
            existing.ReferenceRelatesToAge = item.ReferenceRelatesToAge;
            existing.ReferencerelatesToGyneco = item.ReferencerelatesToGyneco;
            existing.ResultType = item.ResultType;
            existing.UOM = item.UOM;
            existing.IsSelected = item.IsSelected;
            existing.Priority = item.Priority;
            existing.IsRemarkableFactor = item.IsRemarkableFactor;
            existing.ExcludedFromDiscount = item.ExcludedFromDiscount;
            existing.ModifiedDate = DateTime.UtcNow;
            existing.ModifiedBy = item.ModifiedBy;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SoftDeleteAsync(int id)
        {
            var item = await _context.LabTests.FirstOrDefaultAsync(x => x.ID == id);
            
            if (item == null)
                return false;

            item.IsDeleted = true;
            item.ModifiedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            
            return true;
        }
    }
}

