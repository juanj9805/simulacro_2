using Microsoft.EntityFrameworkCore;
using simulationTest.Data;
using simulationTest.Interfaces;
using simulationTest.Models;

namespace simulationTest.Services;

public class TreatmentService : ICrudService<Treatment>
{
    private readonly MysqlDbcontext _context;
    private readonly IEmailService _emailService;

    public TreatmentService(MysqlDbcontext context, IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    public Treatment Create(Treatment entity)
    {
        try
        {
            _context.treatments.Add(entity);
            _context.SaveChanges();

            _ = _emailService.SendAsync(
                "Treatment Assigned - VetCare",
                $"A new treatment has been assigned.\n\nDescription: {entity.Description}\nConsultation ID: {entity.IdConsultation}\nDate: {entity.CreateAt:yyyy-MM-dd HH:mm}"
            );

            return entity;
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("Could not save treatment.", ex);
        }
    }

    public async Task<IEnumerable<Treatment>> GetAllAsync()
    {
        try
        {
            return await _context.treatments
                .Include(t => t.Consultation)
                    .ThenInclude(c => c.Pet)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Could not retrieve treatments.", ex);
        }
    }

    public async Task<Treatment> GetByIdAsync(int id)
    {
        try
        {
            var treatment = await _context.treatments
                .Include(t => t.Consultation)
                    .ThenInclude(c => c.Pet)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (treatment is null)
                throw new KeyNotFoundException($"Treatment with id {id} not found.");

            return treatment;
        }
        catch (KeyNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Could not retrieve treatment.", ex);
        }
    }

    public async Task<Treatment> UpdateAsync(Treatment entity)
    {
        try
        {
            var existing = await _context.treatments.FindAsync(entity.Id)
                ?? throw new KeyNotFoundException($"Treatment with id {entity.Id} not found.");

            existing.IdConsultation = entity.IdConsultation;
            existing.Description = entity.Description;

            await _context.SaveChangesAsync();
            return existing;
        }
        catch (KeyNotFoundException)
        {
            throw;
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("Could not update treatment.", ex);
        }
    }

    public async Task<Treatment> DeleteAsync(int id)
    {
        try
        {
            var treatment = await _context.treatments.FindAsync(id)
                ?? throw new KeyNotFoundException($"Treatment with id {id} not found.");

            _context.treatments.Remove(treatment);
            await _context.SaveChangesAsync();
            return treatment;
        }
        catch (KeyNotFoundException)
        {
            throw;
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("Could not delete treatment.", ex);
        }
    }
}
