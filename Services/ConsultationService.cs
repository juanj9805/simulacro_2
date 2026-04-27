using Microsoft.EntityFrameworkCore;
using simulationTest.Data;
using simulationTest.Interfaces;
using simulationTest.Models;

namespace simulationTest.Services;

public class ConsultationService : ICrudService<Consultation>
{
    private readonly MysqlDbcontext _context;

    public ConsultationService(MysqlDbcontext context)
    {
        _context = context;
    }

    public Consultation Create(Consultation entity)
    {
        try
        {
            _context.consultations.Add(entity);
            _context.SaveChanges();
            return entity;
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("Could not save consultation.", ex);
        }
    }

    public async Task<IEnumerable<Consultation>> GetAllAsync()
    {
        try
        {
            return await _context.consultations
                .Include(c => c.Pet)
                .Include(c => c.Veterinary)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Could not retrieve consultations.", ex);
        }
    }

    public async Task<Consultation> GetByIdAsync(int id)
    {
        try
        {
            var consultation = await _context.consultations
                .Include(c => c.Pet)
                .Include(c => c.Veterinary)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (consultation is null)
                throw new KeyNotFoundException($"Consultation with id {id} not found.");

            return consultation;
        }
        catch (KeyNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Could not retrieve consultation.", ex);
        }
    }

    public async Task<Consultation> UpdateAsync(Consultation entity)
    {
        try
        {
            var existing = await _context.consultations.FindAsync(entity.Id)
                ?? throw new KeyNotFoundException($"Consultation with id {entity.Id} not found.");

            existing.Reason = entity.Reason;
            existing.IdPet = entity.IdPet;
            existing.IdVeterinary = entity.IdVeterinary;
            existing.DateStart = entity.DateStart;
            existing.DateEnd = entity.DateEnd;
            existing.Status = entity.Status;

            await _context.SaveChangesAsync();
            return existing;
        }
        catch (KeyNotFoundException)
        {
            throw;
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("Could not update consultation.", ex);
        }
    }

    public async Task<Consultation> DeleteAsync(int id)
    {
        try
        {
            var consultation = await _context.consultations.FindAsync(id)
                ?? throw new KeyNotFoundException($"Consultation with id {id} not found.");

            _context.consultations.Remove(consultation);
            await _context.SaveChangesAsync();
            return consultation;
        }
        catch (KeyNotFoundException)
        {
            throw;
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("Could not delete consultation.", ex);
        }
    }
}
