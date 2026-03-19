using LIS.Api.Models;

namespace LIS.Api.Repositories
{
    public interface ILabTestsRepository
    {
        Task<List<LabTest>> GetAllAsync();
        Task<LabTest?> GetByIdAsync(int id);
        Task<LabTest> CreateAsync(LabTest item);
        Task<bool> UpdateAsync(LabTest item);
        Task<bool> SoftDeleteAsync(int id);
    }
}







