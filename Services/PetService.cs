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
        try
        {
            _context.pets.Add(entity);
            _context.SaveChanges();
            return entity;
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("Could not save pet.", ex);
        }
    }

    public async Task<IEnumerable<Pet>> GetAllAsync()
    {
        try
        {
            return await _context.pets.Include(p => p.Owner).ToListAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Could not retrieve pets.", ex);
        }
    }

    public async Task<Pet> GetByIdAsync(int id)
    {
        try
        {
            var pet = await _context.pets.Include(p => p.Owner).FirstOrDefaultAsync(p => p.Id == id)
                ?? throw new KeyNotFoundException($"Pet with id {id} not found.");
            return pet;
        }
        catch (KeyNotFoundException) { throw; }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Could not retrieve pet.", ex);
        }
    }

    public async Task<Pet> UpdateAsync(Pet entity)
    {
        try
        {
            var existing = await _context.pets.FindAsync(entity.Id)
                ?? throw new KeyNotFoundException($"Pet with id {entity.Id} not found.");

            existing.Name = entity.Name;
            existing.IdOwner = entity.IdOwner;
            existing.Species = entity.Species;
            existing.Breed = entity.Breed;
            existing.Age = entity.Age;

            await _context.SaveChangesAsync();
            return existing;
        }
        catch (KeyNotFoundException) { throw; }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("Could not update pet.", ex);
        }
    }

    public async Task<Pet> DeleteAsync(int id)
    {
        try
        {
            var pet = await _context.pets.FindAsync(id)
                ?? throw new KeyNotFoundException($"Pet with id {id} not found.");

            _context.pets.Remove(pet);
            await _context.SaveChangesAsync();
            return pet;
        }
        catch (KeyNotFoundException) { throw; }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("Could not delete pet.", ex);
        }
    }
}
