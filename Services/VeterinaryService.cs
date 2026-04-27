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
        try
        {
            _context.veterinaries.Add(entity);
            _context.SaveChanges();
            return entity;
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("Could not save veterinary.", ex);
        }
    }

    public async Task<IEnumerable<Veterinary>> GetAllAsync()
    {
        try
        {
            return await _context.veterinaries.ToListAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Could not retrieve veterinaries.", ex);
        }
    }

    public async Task<Veterinary> GetByIdAsync(int id)
    {
        try
        {
            var vet = await _context.veterinaries.FindAsync(id)
                ?? throw new KeyNotFoundException($"Veterinary with id {id} not found.");
            return vet;
        }
        catch (KeyNotFoundException) { throw; }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Could not retrieve veterinary.", ex);
        }
    }

    public async Task<Veterinary> UpdateAsync(Veterinary entity)
    {
        try
        {
            var existing = await _context.veterinaries.FindAsync(entity.Id)
                ?? throw new KeyNotFoundException($"Veterinary with id {entity.Id} not found.");

            existing.Name = entity.Name;
            existing.Speciality = entity.Speciality;

            await _context.SaveChangesAsync();
            return existing;
        }
        catch (KeyNotFoundException) { throw; }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("Could not update veterinary.", ex);
        }
    }

    public async Task<Veterinary> DeleteAsync(int id)
    {
        try
        {
            var vet = await _context.veterinaries.FindAsync(id)
                ?? throw new KeyNotFoundException($"Veterinary with id {id} not found.");

            _context.veterinaries.Remove(vet);
            await _context.SaveChangesAsync();
            return vet;
        }
        catch (KeyNotFoundException) { throw; }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("Could not delete veterinary.", ex);
        }
    }
}
