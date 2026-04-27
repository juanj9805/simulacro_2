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
    public DbSet<Medicine> medicines { get; set; }    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Pet>()
            .HasOne(p => p.Owner)
            .WithMany(o => o.Pets)
            .HasForeignKey(p => p.IdOwner);

        modelBuilder.Entity<Consultation>()
            .HasOne(c => c.Pet)
            .WithMany()
            .HasForeignKey(c => c.IdPet);

        modelBuilder.Entity<Consultation>()
            .HasOne(c => c.Veterinary)
            .WithMany()
            .HasForeignKey(c => c.IdVeterinary);

        modelBuilder.Entity<Treatment>()
            .HasOne(t => t.Consultation)
            .WithMany()
            .HasForeignKey(t => t.IdConsultation);

        modelBuilder.Entity<TreatmentMedicine>()
            .HasOne(tm => tm.Medicine)
            .WithMany()
            .HasForeignKey(tm => tm.IdMedicine);

        modelBuilder.Entity<TreatmentMedicine>()
            .HasOne(tm => tm.Treatment)
            .WithMany()
            .HasForeignKey(tm => tm.IdTreatment);
    }
}