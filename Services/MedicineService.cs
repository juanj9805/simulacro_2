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

    public Medicine Create(Medicine entity)
    {
        try
        {
            _context.medicines.Add(entity);
            _context.SaveChanges();
            return entity;
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("Could not save medicine.", ex);
        }
    }

    public async Task<IEnumerable<Medicine>> GetAllAsync()
    {
        try
        {
            return await _context.medicines.ToListAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Could not retrieve medicines.", ex);
        }
    }

    public async Task<Medicine> GetByIdAsync(int id)
    {
        try
        {
            var medicine = await _context.medicines.FindAsync(id)
                ?? throw new KeyNotFoundException($"Medicine with id {id} not found.");
            return medicine;
        }
        catch (KeyNotFoundException) { throw; }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Could not retrieve medicine.", ex);
        }
    }

    public async Task<Medicine> UpdateAsync(Medicine entity)
    {
        try
        {
            var existing = await _context.medicines.FindAsync(entity.Id)
                ?? throw new KeyNotFoundException($"Medicine with id {entity.Id} not found.");

            existing.Name = entity.Name;
            existing.Stock = entity.Stock;

            await _context.SaveChangesAsync();
            return existing;
        }
        catch (KeyNotFoundException) { throw; }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("Could not update medicine.", ex);
        }
    }

    public async Task<Medicine> DeleteAsync(int id)
    {
        try
        {
            var medicine = await _context.medicines.FindAsync(id)
                ?? throw new KeyNotFoundException($"Medicine with id {id} not found.");

            _context.medicines.Remove(medicine);
            await _context.SaveChangesAsync();
            return medicine;
        }
        catch (KeyNotFoundException) { throw; }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("Could not delete medicine.", ex);
        }
    }
}
