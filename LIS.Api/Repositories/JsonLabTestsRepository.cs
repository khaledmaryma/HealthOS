using System.Text.Json;
using LIS.Api.Models;

namespace LIS.Api.Repositories
{
    public class JsonLabTestsRepository : ILabTestsRepository
    {
        private readonly string _filePath;
        private readonly object _lock = new();

        public JsonLabTestsRepository(IWebHostEnvironment env)
        {
            var dir = Path.Combine(env.ContentRootPath, "App_Data");
            Directory.CreateDirectory(dir);
            _filePath = Path.Combine(dir, "labtests.json");
            if (!File.Exists(_filePath))
            {
                File.WriteAllText(_filePath, "[]");
            }
        }

        public Task<List<LabTest>> GetAllAsync()
        {
            var list = Read();
            return Task.FromResult(list.Where(x => !x.IsDeleted).OrderBy(x => x.DisplayOrder).ThenBy(x => x.TestDesciption).ToList());
        }

        public Task<LabTest?> GetByIdAsync(int id)
        {
            var list = Read();
            return Task.FromResult(list.FirstOrDefault(x => x.ID == id && !x.IsDeleted));
        }

        public Task<LabTest> CreateAsync(LabTest item)
        {
            lock (_lock)
            {
                var list = Read();
                item.ID = list.Count == 0 ? 1 : list.Max(x => x.ID) + 1;
                item.CreatedDate = DateTime.UtcNow;
                list.Add(item);
                Write(list);
                return Task.FromResult(item);
            }
        }

        public Task<bool> UpdateAsync(LabTest item)
        {
            lock (_lock)
            {
                var list = Read();
                var existing = list.FirstOrDefault(x => x.ID == item.ID);
                if (existing == null || existing.IsDeleted) return Task.FromResult(false);

                Console.WriteLine($"Repository Update - Before: {existing.DefaultTextResult ?? "NULL"}");
                
                existing.Code = item.Code;
                existing.TestDesciption = item.TestDesciption;
                existing.DefaultTextResult = item.DefaultTextResult;
                existing.DisplayOrder = item.DisplayOrder;
                existing.IsACollection = item.IsACollection;
                existing.HasReferenceRange = item.HasReferenceRange;
                existing.ReferenceRelatesToAge = item.ReferenceRelatesToAge;
                existing.ReferencerelatesToGyneco = item.ReferencerelatesToGyneco;
                existing.ResultType = item.ResultType;
                existing.IsSelected = item.IsSelected;
                existing.Priority = item.Priority;
                existing.IsRemarkableFactor = item.IsRemarkableFactor;
                existing.ExcludedFromDiscount = item.ExcludedFromDiscount;
                existing.ModifiedBy = item.ModifiedBy;
                existing.ModifiedDate = DateTime.UtcNow;

                Console.WriteLine($"Repository Update - After: {existing.DefaultTextResult ?? "NULL"}");
                
                Write(list);
                return Task.FromResult(true);
            }
        }

        public Task<bool> SoftDeleteAsync(int id)
        {
            lock (_lock)
            {
                var list = Read();
                var existing = list.FirstOrDefault(x => x.ID == id);
                if (existing == null || existing.IsDeleted) return Task.FromResult(false);
                existing.IsDeleted = true;
                existing.ModifiedDate = DateTime.UtcNow;
                Write(list);
                return Task.FromResult(true);
            }
        }

        private List<LabTest> Read()
        {
            if (!File.Exists(_filePath)) return new List<LabTest>();
            var json = File.ReadAllText(_filePath);
            var list = JsonSerializer.Deserialize<List<LabTest>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<LabTest>();
            
            if (list.Any())
            {
                Console.WriteLine($"Read {list.Count} items from JSON");
                Console.WriteLine($"First item DefaultTextResult: {list.First().DefaultTextResult ?? "NULL"}");
            }
            
            return list;
        }

        private void Write(List<LabTest> list)
        {
            var options = new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = null, // Use exact property names
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never // Include null values
            };
            var json = JsonSerializer.Serialize(list, options);
            File.WriteAllText(_filePath, json);
            Console.WriteLine("JSON file written - all fields included");
        }
    }
}





