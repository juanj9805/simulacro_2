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
        var ser = await _service.GetAllAsync();
        return View(ser);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Create(Veterinary veterinary)
    {
        _service.Create(veterinary);
        return RedirectToAction("Index");
    }
}