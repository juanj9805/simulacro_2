using Microsoft.AspNetCore.Mvc;
using simulationTest.Models;
using simulationTest.Services;

namespace simulationTest.Controllers;

public class MedicineController : Controller
{
    private readonly MedicineService _service;

    public MedicineController(MedicineService service)
    {
        _service = service;
    }

    public async Task<IActionResult> Index()
    {
        var medicines = await _service.GetAllAsync();
        return View(medicines);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Create(Medicine medicine)
    {
        if (!ModelState.IsValid)
            return View(medicine);

        try
        {
            _service.Create(medicine);
            return RedirectToAction("Index");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(medicine);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Update(int id)
    {
        try
        {
            var medicine = await _service.GetByIdAsync(id);
            return View(medicine);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost]
    public async Task<IActionResult> Update(Medicine medicine)
    {
        if (!ModelState.IsValid)
            return View(medicine);

        try
        {
            await _service.UpdateAsync(medicine);
            return RedirectToAction("Index");
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(medicine);
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
