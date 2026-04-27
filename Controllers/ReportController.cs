using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using simulationTest.Data;
using simulationTest.Models;

namespace simulationTest.Controllers;

public class ReportController : Controller
{
    private readonly MysqlDbcontext _context;

    public ReportController(MysqlDbcontext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var consultations = await _context.consultations
            .Include(c => c.Pet)
            .Include(c => c.Veterinary)
            .ToListAsync();

        ViewBag.TopVets = consultations
            .GroupBy(c => c.Veterinary?.Name ?? "Unknown")
            .Select(g => new { Name = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToList();

        ViewBag.TopPets = consultations
            .GroupBy(c => c.Pet?.Name ?? "Unknown")
            .Select(g => new { Name = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToList();

        var medicines = await _context.treatmentsMedicines
            .Include(tm => tm.Medicine)
            .ToListAsync();

        ViewBag.TopMedicines = medicines
            .GroupBy(tm => tm.Medicine?.Name ?? "Unknown")
            .Select(g => new { Name = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToList();

        var total = consultations.Count;
        var noShows = consultations.Count(c => c.Status == Status.NoShow);
        ViewBag.Total = total;
        ViewBag.NoShows = noShows;
        ViewBag.NoShowRate = total > 0 ? Math.Round((double)noShows / total * 100, 1) : 0;

        return View();
    }
}
