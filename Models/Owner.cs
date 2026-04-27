using System.ComponentModel.DataAnnotations;

namespace simulationTest.Models;

public class Owner
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public IEnumerable<Pet> Pets { get; set; } = [];
}