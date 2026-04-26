namespace simulationTest.Models;

public class Treatment
{
    public int Id { get; set; }
    public int IdConsultation { get; set; }
    public Consultation Consultation { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime CreateAt { get; set; } = DateTime.UtcNow;
    
}