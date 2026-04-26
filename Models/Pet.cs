namespace simulationTest.Models;

public class Pet
{
    public int Id { get; set; }
    public int IdOwner { get; set; }
    public Owner Owner { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Breed { get; set; } = string.Empty;
    public int Age { get; set; }
}