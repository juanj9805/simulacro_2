namespace simulationTest.Models;

public class TreatmentMedicine
{
    public int Id { get; set; }
    public int IdMedicine { get; set; }
    public Medicine? Medicine { get; set; }
    public int IdTreatment { get; set; }
    public Treatment? Treatment { get; set; }
    
}