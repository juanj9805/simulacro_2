using Microsoft.EntityFrameworkCore;
using simulationTest.Models;

namespace simulationTest.Data;

public class MysqlDbcontext : DbContext
{
    public MysqlDbcontext(DbContextOptions<MysqlDbcontext> options) : base(options)
    {
    }
    
    public DbSet<Owner> owners { get; set; }    
    public DbSet<Pet> pets { get; set; }    
    public DbSet<Consultation> consultations { get; set; }    
    public DbSet<Veterinary> veterinaries { get; set; }    
    public DbSet<Treatment> treatments { get; set; }    
    public DbSet<TreatmentMedicine> treatmentsMedicines { get; set; }    
    public DbSet<Medicine> Medicines { get; set; }    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
    }
}