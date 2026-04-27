using Microsoft.AspNetCore.Mvc;
using simulationTest.Models;
using simulationTest.Services;

namespace simulationTest.Controllers;

public class OwnerController : Controller
{
    private readonly OwnerService _service;

    public OwnerController(OwnerService service)
    {
        _service = service;
    }

    public async Task<IActionResult> Index()
    {
        var owners = await _service.GetAllAsync();
        return View(owners);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Create(Owner owner)
    {
        if (!ModelState.IsValid)
            return View(owner);

        try
        {
            _service.Create(owner);
            return RedirectToAction("Index");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(owner);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Update(int id)
    {
        try
        {
            var owner = await _service.GetByIdAsync(id);
            return View(owner);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost]
    public async Task<IActionResult> Update(Owner owner)
    {
        if (!ModelState.IsValid)
            return View(owner);

        try
        {
            await _service.UpdateAsync(owner);
            return RedirectToAction("Index");
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(owner);
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
