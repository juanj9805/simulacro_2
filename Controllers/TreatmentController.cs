using Microsoft.AspNetCore.Mvc;
using simulationTest.Models;
using simulationTest.Services;

namespace simulationTest.Controllers;

public class TreatmentController : Controller
{
    private readonly TreatmentService _service;
    private readonly ConsultationService _consultationService;

    public TreatmentController(TreatmentService service, ConsultationService consultationService)
    {
        _service = service;
        _consultationService = consultationService;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var treatments = await _service.GetAllAsync();
            return View(treatments);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(Enumerable.Empty<Treatment>());
        }
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadDropdownsAsync();
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(Treatment treatment)
    {
        if (!ModelState.IsValid)
        {
            await LoadDropdownsAsync();
            return View(treatment);
        }

        try
        {
            _service.Create(treatment);
            return RedirectToAction("Index");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await LoadDropdownsAsync();
            return View(treatment);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Update(int id)
    {
        try
        {
            var treatment = await _service.GetByIdAsync(id);
            await LoadDropdownsAsync();
            return View(treatment);
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
    public async Task<IActionResult> Update(Treatment treatment)
    {
        if (!ModelState.IsValid)
        {
            await LoadDropdownsAsync();
            return View(treatment);
        }

        try
        {
            await _service.UpdateAsync(treatment);
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
            return View(treatment);
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
        ViewBag.Consultations = await _consultationService.GetAllAsync();
    }
}
