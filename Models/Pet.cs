namespace simulationTest.Models;

public enum Species {Cat, Dog, Other}
public class Pet
{
    public int Id { get; set; }
    public int IdOwner { get; set; }
    public Owner? Owner { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Breed { get; set; } = string.Empty;
    public Species Species { get; set; }
    public int Age { get; set; }
}