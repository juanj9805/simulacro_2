using Microsoft.EntityFrameworkCore;
using simulationTest.Data;
using simulationTest.Interfaces;
using simulationTest.Models;
using simulationTest.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<MysqlDbcontext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("MysqlConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("MysqlConnection"))));

var emailSettings = builder.Configuration.GetSection("EmailSettings").Get<EmailSettings>()!;
builder.Services.AddSingleton(emailSettings);
builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.AddScoped<OwnerService>();
builder.Services.AddScoped<MedicineService>();
builder.Services.AddScoped<PetService>();
builder.Services.AddScoped<VeterinaryService>();
builder.Services.AddScoped<ConsultationService>();
builder.Services.AddScoped<TreatmentService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
