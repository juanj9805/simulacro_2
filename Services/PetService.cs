using Microsoft.EntityFrameworkCore;
using simulationTest.Data;
using simulationTest.Interfaces;
using simulationTest.Models;

namespace simulationTest.Services;

public class PetService : ICrudService<Pet>
{
    private readonly MysqlDbcontext _context;

    public PetService(MysqlDbcontext context)
    {
        _context = context;
    }
    
    public Pet Create(Pet entity)
    {
        _context.pets.Add(entity);
        _context.SaveChanges();
        return entity;
    }

    public async Task<IEnumerable<Pet>> GetAllAsync()
    {
        var pets = await _context.pets.ToListAsync();
        return pets;
    }

    public Task<Pet> GetByIdAsync(int id)
    {
        throw new NotImplementedException();
    }

    public Task<Pet> UpdateAsync(Pet entity)
    {
        throw new NotImplementedException();
    }

    public Task<Pet> DeleteAsync(int id)
    {
        throw new NotImplementedException();
    }
}