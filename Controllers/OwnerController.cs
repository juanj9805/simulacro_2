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
        var ser = await _service.GetAllAsync();
        return View(ser);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Create(Owner owner)
    {
        _service.Create(owner);
        return RedirectToAction("Index");
    }
}