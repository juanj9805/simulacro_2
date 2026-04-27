using Microsoft.AspNetCore.Mvc;
using simulationTest.Models;
using simulationTest.Services;

namespace simulationTest.Controllers;

public class PetController : Controller
{
    private readonly PetService _service;
    private readonly OwnerService _ownerService;

    public PetController(PetService service, OwnerService ownerService)
    {
        _service = service;
        _ownerService = ownerService;
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

        _service.Create(pet);
        return RedirectToAction("Index");
    }
}
