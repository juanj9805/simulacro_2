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

    public Owner Create(Owner entity)
    {
        try
        {
            _context.owners.Add(entity);
            _context.SaveChanges();
            return entity;
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("Could not save owner.", ex);
        }
    }

    public async Task<IEnumerable<Owner>> GetAllAsync()
    {
        try
        {
            return await _context.owners.ToListAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Could not retrieve owners.", ex);
        }
    }

    public async Task<Owner> GetByIdAsync(int id)
    {
        try
        {
            var owner = await _context.owners.FindAsync(id)
                ?? throw new KeyNotFoundException($"Owner with id {id} not found.");
            return owner;
        }
        catch (KeyNotFoundException) { throw; }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Could not retrieve owner.", ex);
        }
    }

    public async Task<Owner> UpdateAsync(Owner entity)
    {
        try
        {
            var existing = await _context.owners.FindAsync(entity.Id)
                ?? throw new KeyNotFoundException($"Owner with id {entity.Id} not found.");

            existing.Name = entity.Name;
            existing.Phone = entity.Phone;

            await _context.SaveChangesAsync();
            return existing;
        }
        catch (KeyNotFoundException) { throw; }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("Could not update owner.", ex);
        }
    }

    public async Task<Owner> DeleteAsync(int id)
    {
        try
        {
            var owner = await _context.owners.FindAsync(id)
                ?? throw new KeyNotFoundException($"Owner with id {id} not found.");

            _context.owners.Remove(owner);
            await _context.SaveChangesAsync();
            return owner;
        }
        catch (KeyNotFoundException) { throw; }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("Could not delete owner.", ex);
        }
    }
}
