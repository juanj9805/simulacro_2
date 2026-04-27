using Microsoft.AspNetCore.Mvc;
using simulationTest.Models;
using simulationTest.Services;

namespace simulationTest.Controllers;

public class ConsultationController : Controller
{
    private readonly ConsultationService _service;
    private readonly PetService _petService;
    private readonly VeterinaryService _vetService;

    public ConsultationController(ConsultationService service, PetService petService, VeterinaryService vetService)
    {
        _service = service;
        _petService = petService;
        _vetService = vetService;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var consultations = await _service.GetAllAsync();
            return View(consultations);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(Enumerable.Empty<Consultation>());
        }
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadDropdownsAsync();
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(Consultation consultation)
    {
        if (!ModelState.IsValid)
        {
            await LoadDropdownsAsync();
            return View(consultation);
        }

        try
        {
            _service.Create(consultation);
            return RedirectToAction("Index");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await LoadDropdownsAsync();
            return View(consultation);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Update(int id)
    {
        try
        {
            var consultation = await _service.GetByIdAsync(id);
            await LoadDropdownsAsync();
            return View(consultation);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return RedirectToAction("Index");
        }
    }

    [HttpPost]
    public async Task<IActionResult> Update(Consultation consultation)
    {
        if (!ModelState.IsValid)
        {
            await LoadDropdownsAsync();
            return View(consultation);
        }

        try
        {
            await _service.UpdateAsync(consultation);
            return RedirectToAction("Index");
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await LoadDropdownsAsync();
            return View(consultation);
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
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return RedirectToAction("Index");
        }
    }

    private async Task LoadDropdownsAsync()
    {
        ViewBag.Pets = await _petService.GetAllAsync();
        ViewBag.Veterinaries = await _vetService.GetAllAsync();
    }
}
