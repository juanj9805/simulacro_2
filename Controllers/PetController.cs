using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using simulationTest.Data;
using simulationTest.Models;
using simulationTest.Services;

namespace simulationTest.Controllers;

public class PetController : Controller
{
    private readonly PetService _service;
    private readonly OwnerService _ownerService;
    private readonly MysqlDbcontext _context;

    public PetController(PetService service, OwnerService ownerService, MysqlDbcontext context)
    {
        _service = service;
        _ownerService = ownerService;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var pets = await _service.GetAllAsync();
        return View(pets);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewBag.Owners = await _ownerService.GetAllAsync();
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(Pet pet)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Owners = await _ownerService.GetAllAsync();
            return View(pet);
        }

        try
        {
            _service.Create(pet);
            return RedirectToAction("Index");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            ViewBag.Owners = await _ownerService.GetAllAsync();
            return View(pet);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Update(int id)
    {
        try
        {
            var pet = await _service.GetByIdAsync(id);
            ViewBag.Owners = await _ownerService.GetAllAsync();
            return View(pet);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost]
    public async Task<IActionResult> Update(Pet pet)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Owners = await _ownerService.GetAllAsync();
            return View(pet);
        }

        try
        {
            await _service.UpdateAsync(pet);
            return RedirectToAction("Index");
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            ViewBag.Owners = await _ownerService.GetAllAsync();
            return View(pet);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _service.DeleteAsync(id);
            return RedirectToAction("Index");
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet]
    public async Task<IActionResult> History(int id)
    {
        try
        {
            var pet = await _service.GetByIdAsync(id);

            var consultations = await _context.consultations
                .Where(c => c.IdPet == id)
                .Include(c => c.Veterinary)
                .OrderByDescending(c => c.DateStart)
                .ToListAsync();

            var consultationIds = consultations.Select(c => c.Id).ToList();

            var treatments = await _context.treatments
                .Where(t => consultationIds.Contains(t.IdConsultation))
                .ToListAsync();

            ViewBag.Pet = pet;
            ViewBag.Consultations = consultations;
            ViewBag.Treatments = treatments;

            return View();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}
