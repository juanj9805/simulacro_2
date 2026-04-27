using Microsoft.EntityFrameworkCore;
using simulationTest.Data;
using simulationTest.Interfaces;
using simulationTest.Models;

namespace simulationTest.Services;

public class VeterinaryService : ICrudService<Veterinary>
{
    private readonly MysqlDbcontext _context;

    public VeterinaryService(MysqlDbcontext context)
    {
        _context = context;
    }
    
    public Veterinary Create(Veterinary entity)
    {
        _context.veterinaries.Add(entity);
        _context.SaveChanges();
        return entity;
    }

    public async Task<IEnumerable<Veterinary>> GetAllAsync()
    {
        var vets = await _context.veterinaries.ToListAsync();
        return vets;
    }

    public Task<Veterinary> GetByIdAsync(int id)
    {
        throw new NotImplementedException();
    }

    public Task<Veterinary> UpdateAsync(Veterinary entity)
    {
        throw new NotImplementedException();
    }

    public Task<Veterinary> DeleteAsync(int id)
    {
        throw new NotImplementedException();
    }
}