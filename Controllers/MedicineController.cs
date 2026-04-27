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
        var ser = await _service.GetAllAsync();
        return View(ser);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Create(Medicine medicine)
    {
        _service.Create(medicine);
        return RedirectToAction("Index");
    }
}