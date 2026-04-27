using Microsoft.EntityFrameworkCore;
using simulationTest.Data;
using simulationTest.Interfaces;
using simulationTest.Models;

namespace simulationTest.Services;

public class ConsultationService : ICrudService<Consultation>
{
    private readonly MysqlDbcontext _context;
    private readonly IEmailService _emailService;

    public ConsultationService(MysqlDbcontext context, IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    public Consultation Create(Consultation entity)
    {
        if (entity.DateStart < DateTime.Now)
            throw new InvalidOperationException("Cannot schedule an appointment in the past.");

        if (entity.DateEnd <= entity.DateStart)
            throw new InvalidOperationException("End time must be after start time.");

        var activeCount = _context.consultations.Count(c =>
            c.IdPet == entity.IdPet && c.Status == Status.Scheduled);
        if (activeCount >= 2)
            throw new InvalidOperationException("This pet already has 2 active appointments.");

        var noShowCount = _context.consultations.Count(c =>
            c.IdPet == entity.IdPet && c.Status == Status.NoShow);
        if (noShowCount >= 3)
        {
            var lastNoShow = _context.consultations
                .Where(c => c.IdPet == entity.IdPet && c.Status == Status.NoShow)
                .OrderByDescending(c => c.DateStart)
                .First();
            if (lastNoShow.DateStart >= DateTime.Now.AddDays(-7))
                throw new InvalidOperationException("This pet is blocked from new appointments for 7 days due to 3 no-shows.");
        }

        var vetOverlap = _context.consultations.Any(c =>
            c.IdVeterinary == entity.IdVeterinary &&
            c.Status == Status.Scheduled &&
            c.DateStart < entity.DateEnd &&
            c.DateEnd > entity.DateStart);
        if (vetOverlap)
            throw new InvalidOperationException("The veterinarian already has an appointment at this time.");

        var petOverlap = _context.consultations.Any(c =>
            c.IdPet == entity.IdPet &&
            c.Status == Status.Scheduled &&
            c.DateStart < entity.DateEnd &&
            c.DateEnd > entity.DateStart);
        if (petOverlap)
            throw new InvalidOperationException("This pet already has an appointment at this time.");

        try
        {
            _context.consultations.Add(entity);
            _context.SaveChanges();

            _ = _emailService.SendAsync(
                "Appointment Created - VetCare",
                $"A new appointment has been scheduled.\n\nReason: {entity.Reason}\nDate: {entity.DateStart:yyyy-MM-dd HH:mm} to {entity.DateEnd:HH:mm}\nStatus: {entity.Status}"
            );

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
        catch (KeyNotFoundException) { throw; }
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

            var previousStatus = existing.Status;

            existing.Reason = entity.Reason;
            existing.IdPet = entity.IdPet;
            existing.IdVeterinary = entity.IdVeterinary;
            existing.DateStart = entity.DateStart;
            existing.DateEnd = entity.DateEnd;
            existing.Status = entity.Status;

            await _context.SaveChangesAsync();

            if (previousStatus != Status.Canceled && existing.Status == Status.Canceled)
            {
                _ = _emailService.SendAsync(
                    "Appointment Cancelled - VetCare",
                    $"An appointment has been cancelled.\n\nReason: {existing.Reason}\nDate: {existing.DateStart:yyyy-MM-dd HH:mm}"
                );
            }

            return existing;
        }
        catch (KeyNotFoundException) { throw; }
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
        catch (KeyNotFoundException) { throw; }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("Could not delete consultation.", ex);
        }
    }
}
