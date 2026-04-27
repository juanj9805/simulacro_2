using Microsoft.EntityFrameworkCore;
using simulationTest.Data;
using simulationTest.Interfaces;
using simulationTest.Models;

namespace simulationTest.Services;

public class MedicineService : ICrudService<Medicine>
{
    private readonly MysqlDbcontext _context;

    public MedicineService(MysqlDbcontext context)
    {
        _context = context;
    }
    
    public  Medicine Create(Medicine entity)
    {
        _context.medicines.Add(entity);
        _context.SaveChanges();
        return entity;
    }

    public async Task<IEnumerable<Medicine>> GetAllAsync()
    {
        var medicines = await _context.medicines.ToListAsync();
        return medicines;
    }

    public Task<Medicine> GetByIdAsync(int id)
    {
        throw new NotImplementedException();
    }

    public Task<Medicine> UpdateAsync(Medicine entity)
    {
        throw new NotImplementedException();
    }

    public Task<Medicine> DeleteAsync(int id)
    {
        throw new NotImplementedException();
    }
}