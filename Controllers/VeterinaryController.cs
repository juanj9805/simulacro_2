using Microsoft.AspNetCore.Mvc;
using simulationTest.Models;
using simulationTest.Services;

namespace simulationTest.Controllers;

public class VeterinaryController : Controller
{
    private readonly VeterinaryService _service;

    public VeterinaryController(VeterinaryService service)
    {
        _service = service;
    }

    public async Task<IActionResult> Index()
    {
        var vets = await _service.GetAllAsync();
        return View(vets);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Create(Veterinary veterinary)
    {
        if (!ModelState.IsValid)
            return View(veterinary);

        try
        {
            _service.Create(veterinary);
            return RedirectToAction("Index");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(veterinary);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Update(int id)
    {
        try
        {
            var vet = await _service.GetByIdAsync(id);
            return View(vet);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost]
    public async Task<IActionResult> Update(Veterinary veterinary)
    {
        if (!ModelState.IsValid)
            return View(veterinary);

        try
        {
            await _service.UpdateAsync(veterinary);
            return RedirectToAction("Index");
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(veterinary);
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
}
