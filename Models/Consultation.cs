namespace simulationTest.Models;

public class Consultation
{
    public int Id { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int IdPet { get; set; }
    public Pet Pet { get; set; }
    public int IdVeterinary { get; set; }
    public Veterinary Veterinary{ get; set; }
    public DateTime DateStart { get; set; }
    public DateTime DateEnd { get; set; }
}