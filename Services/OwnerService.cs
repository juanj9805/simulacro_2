using Microsoft.EntityFrameworkCore;
using simulationTest.Data;
using simulationTest.Interfaces;
using simulationTest.Models;

namespace simulationTest.Services;

public class OwnerService : ICrudService<Owner>
{
    private readonly MysqlDbcontext _context;

    public OwnerService(MysqlDbcontext context)
    {
        _context = context;
    }
    
    public  Owner Create(Owner entity)
    {
        _context.owners.Add(entity);
        _context.SaveChanges();
        return entity;
    }

    public async Task<IEnumerable<Owner>> GetAllAsync()
    {
        var owners = await _context.owners.ToListAsync();
        return owners;
    }

    public Task<Owner> GetByIdAsync(int id)
    {
        throw new NotImplementedException();
    }

    public Task<Owner> UpdateAsync(Owner entity)
    {
        throw new NotImplementedException();
    }

    public Task<Owner> DeleteAsync(int id)
    {
        throw new NotImplementedException();
    }
}