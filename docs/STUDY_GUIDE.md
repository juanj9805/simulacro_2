# VetCare Management System — Study Guide

A comprehensive technical walkthrough of every layer of this project. Use this as both a reference while you continue building and a refresher before interviews. Every section contains real code from the project, the reasoning behind the decisions, the bugs that were encountered, and exercises to verify your understanding.

---

## Table of Contents

### Part I — Foundations
1. [Project Overview](#1-project-overview)
2. [The Layered Architecture](#2-the-layered-architecture)

### Part II — Data Layer
3. [Models and Entities](#3-models-and-entities)
4. [EF Core Deep Dive](#4-ef-core-deep-dive)
5. [Migrations](#5-migrations)

### Part III — Application Layer
6. [The Service Layer and ICrudService](#6-the-service-layer-and-icrudservice)
7. [Controllers and HTTP Handling](#7-controllers-and-http-handling)
8. [Views with Razor](#8-views-with-razor)

### Part IV — Business Logic
9. [Form Validation and ModelState](#9-form-validation-and-modelstate)
10. [Business Rules in ConsultationService](#10-business-rules-in-consultationservice)
11. [Error Handling Strategy](#11-error-handling-strategy)

### Part V — Cross-Cutting Concerns
12. [SMTP Email with MailKit](#12-smtp-email-with-mailkit)
13. [Reports and LINQ Aggregation](#13-reports-and-linq-aggregation)
14. [Medical History](#14-medical-history)

### Part VI — Real Bugs Encountered
15. [The Bug Catalog](#15-the-bug-catalog)

### Part VII — Production and Career
16. [Production-Readiness Gaps and Roadmap](#16-production-readiness-gaps-and-roadmap)

---

## 1. Project Overview

### What it is

VetCare is a clinic management system for veterinarians. The clinic was managing operations manually with paper agendas and spreadsheets, which created six concrete problems:

1. Appointments overlapping on the same vet or the same pet
2. Pets without a clear clinical history
3. No control over medicine inventory
4. Disorganized vet assignments
5. Information loss
6. No way to generate reports

VetCare replaces those manual processes with a database-backed web application that enforces the business rules at the data layer instead of relying on people to remember them.

### How it's used in this project

The system has eight functional modules, each represented by a controller and one or more services:

| Module | Entity | Controller | Service |
|---|---|---|---|
| Owners | `Owner` | `OwnerController` | `OwnerService` |
| Pets | `Pet` | `PetController` | `PetService` |
| Veterinarians | `Veterinary` | `VeterinaryController` | `VeterinaryService` |
| Medicines | `Medicine` | `MedicineController` | `MedicineService` |
| Consultations | `Consultation` | `ConsultationController` | `ConsultationService` |
| Treatments | `Treatment` | `TreatmentController` | `TreatmentService` |
| Reports | (multi-entity LINQ) | `ReportController` | (uses DbContext directly) |
| Notifications | (cross-cutting) | (no controller) | `EmailService` |

The tech stack is:

- **.NET 10** — runtime and language version
- **ASP.NET Core MVC** — web framework with controllers, views, and routing
- **EF Core 9** — ORM for database access
- **Pomelo.EntityFrameworkCore.MySql 9** — MySQL provider for EF Core
- **MailKit 4** — SMTP client library for sending emails
- **Bootstrap 5** — UI styling (via the existing `_Layout.cshtml` and the custom CSS in `simulacro.styles.css`)

### Why this approach

A web application was chosen instead of a desktop app because:

1. The clinic likely needs multi-device access (front desk computer, vet tablets, owner kiosk)
2. Web apps deploy in one place and update without reinstalling
3. ASP.NET Core MVC has mature scaffolding for forms, validation, and CRUD which matches the project's needs

MySQL was chosen over PostgreSQL because the requirement document explicitly listed both as acceptable. MySQL is more common in shared hosting and small clinics may already use it for billing software.

The MVC pattern was the natural fit because:

- The clinic staff interacts with HTML pages, not a SPA
- Server-side rendering keeps the application accessible without JavaScript
- Razor views integrate tightly with the model and controller, reducing boilerplate

### Common mistakes

A common beginner mistake when planning a system like this is to start coding controllers before the data model is settled. The data model dictates everything else — relationships drive form fields, form fields drive views, views drive controller actions. Spending half a day drawing the entity relationships on paper saves a week of refactoring later.

Another mistake is treating the requirements document as a checklist rather than a contract. The requirement *"3 inasistencias → bloqueo de nuevas citas por 7 días"* sounds simple but actually requires you to:

1. Detect that a pet has 3 no-shows
2. Determine the date of the most recent no-show
3. Check whether the current moment is within 7 days of that date
4. Apply the block consistently in every code path that creates a consultation

If you only handle the happy path — "create the consultation if no validation fails" — you'll miss the rule.

### Improvements for production

This project is functional but several pieces would need to change before production:

- **Authentication and authorization**: Anyone can currently delete owners or modify consultations. A real clinic needs role-based access (admin, vet, receptionist).
- **Audit logging**: Who created or cancelled this appointment? When? The current system doesn't track this.
- **Soft deletes**: A deleted pet should probably be archived, not removed. If a customer asks for the medical history of a deceased pet, you don't want to have purged it.
- **Backup strategy**: A clinic's database is the clinic's memory. Daily backups are the floor.
- **HTTPS in production**: Currently configured via `app.UseHttpsRedirection()` but the certificate handling is minimal.

### Exercises

**1. Predict.** A receptionist creates an appointment for tomorrow at 10:00 AM. The system already has an appointment for the same vet at 11:00 AM. New appointment ends at 11:30 AM. Will the system allow it? Walk through which business rule fires.

<details>
<summary>Solution</summary>

The system blocks it. The vet overlap rule in `ConsultationService.Create` checks: `c.DateStart < entity.DateEnd && c.DateEnd > entity.DateStart`. The existing 11:00 AM appointment has `DateStart = 11:00`, the new one ends at `DateEnd = 11:30`. So `11:00 < 11:30` is true. The existing appointment ends at `12:00` (assuming 1 hour), so `12:00 > 10:00` is true. Both conditions met → overlap detected → `InvalidOperationException` thrown.
</details>

**2. Diagnose.** A clinic owner reports that the no-show block isn't working. They have a pet with 4 no-show appointments and the system still allows new bookings. Where would you look first?

<details>
<summary>Solution</summary>

First check `ConsultationService.Create` — the rule queries no-show consultations and checks the date of the last one. The bug is likely that the date comparison uses `DateTime.UtcNow` while the consultations were saved in local time, so the "within 7 days" check evaluates incorrectly. Verify the time zones first, then check whether `Status.NoShow` is actually being set on missed appointments (someone may be leaving them as `Scheduled`).
</details>

**3. Implement.** A new requirement: a pet cannot have more than 5 lifetime consultations across all vets. Where in the codebase do you add this rule? Sketch the code.

<details>
<summary>Solution</summary>

In `ConsultationService.Create`, before the existing rules:

```csharp
var lifetimeCount = _context.consultations.Count(c => c.IdPet == entity.IdPet);
if (lifetimeCount >= 5)
    throw new InvalidOperationException("This pet has reached the lifetime appointment limit.");
```

It belongs in the service, not the controller, because it's a business rule that should apply regardless of how the request arrives (web form, future API, batch import).
</details>

---

## 2. The Layered Architecture

### What it is

The project follows a classic 4-layer architecture:

```
┌─────────────────────────────────────┐
│  View (Razor cshtml)                │  ← What the user sees
└─────────────────────────────────────┘
              │ data passed as Model + ViewBag
              ▼
┌─────────────────────────────────────┐
│  Controller (HTTP layer)            │  ← Receives requests, returns responses
└─────────────────────────────────────┘
              │ method calls
              ▼
┌─────────────────────────────────────┐
│  Service (Business logic)           │  ← Validates rules, orchestrates work
└─────────────────────────────────────┘
              │ DbContext queries
              ▼
┌─────────────────────────────────────┐
│  Model + DbContext (Data)           │  ← Entities + persistence
└─────────────────────────────────────┘
              │ SQL
              ▼
         MySQL Database
```

Each layer has one responsibility and only talks to the layer immediately below it. The View never talks to the Service. The Controller never writes SQL. The Service never sees `HttpContext`.

### How it's used in this project

Look at any complete CRUD flow to see all four layers in action. Take owner creation:

1. **View** (`Views/Owner/Create.cshtml`) — renders an HTML form bound to the `Owner` model
2. **Controller** (`Controllers/OwnerController.cs`) — receives the POST, validates the form via `ModelState.IsValid`, calls the service
3. **Service** (`Services/OwnerService.cs`) — adds the entity to the DbContext, calls `SaveChanges`, translates DB exceptions to domain exceptions
4. **DbContext** (`Data/MysqlDbcontext.cs`) — sends the `INSERT` statement to MySQL

Here's the full controller method, showing the layer boundaries:

```csharp
[HttpPost]
public IActionResult Create(Owner owner)
{
    if (!ModelState.IsValid)
        return View(owner);

    try
    {
        _service.Create(owner);
        return RedirectToAction("Index");
    }
    catch (InvalidOperationException ex)
    {
        ModelState.AddModelError(string.Empty, ex.Message);
        return View(owner);
    }
}
```

Notice three things:

- The controller calls `_service.Create(owner)` — it doesn't touch `_context.owners.Add()` directly. That would skip the service layer.
- The controller catches `InvalidOperationException` — the domain exception type that the service throws. It does *not* catch `DbUpdateException` because that's an EF Core concern that should never leak past the service.
- The controller decides what HTTP response to return (`RedirectToAction`, `View(owner)`). The service has no opinion on HTTP.

### Why this approach

ASP.NET Core MVC provides Models, Views, and Controllers out of the box. Why add a Service layer?

**Without a service layer**, controllers do everything: HTTP parsing, validation, business rules, database access. Three problems:

1. **Untestable**. To test a business rule, you have to instantiate a controller, fake an `HttpContext`, fake `ModelState`, and run an action. Testing is painful, so it doesn't get done.
2. **Unreusable**. If you later add a REST API or a CLI tool, you have to copy-paste the business logic from the MVC controller into a new place.
3. **Bloated**. Controllers grow into 500-line files that mix request validation with database operations.

**With a service layer**, the controller is a thin wrapper that translates between HTTP and method calls. The service contains the actual logic and can be tested in isolation. Adding an API later means writing a new controller that calls the same service — the rules don't move.

The dependency direction is enforced by **Dependency Injection** registered in `Program.cs`:

```csharp
builder.Services.AddScoped<OwnerService>();
builder.Services.AddScoped<MedicineService>();
builder.Services.AddScoped<PetService>();
builder.Services.AddScoped<VeterinaryService>();
builder.Services.AddScoped<ConsultationService>();
builder.Services.AddScoped<TreatmentService>();
```

Each service is registered with **Scoped** lifetime, meaning one instance per HTTP request. This matches the lifetime of `MysqlDbcontext`, which must also be scoped (you don't want one DbContext shared across all users).

### Common mistakes

The most common architectural mistake is **leaking concerns across layers**:

- A controller that calls `_context.owners.Add(...)` directly — bypasses the service
- A service that returns `IActionResult` — service knows about HTTP, no longer reusable
- A service that takes an `HttpContext` parameter — same problem
- A view that queries the database via injected DbContext — bypasses the controller and service entirely
- A model with logic that talks to the database — entity is now coupled to persistence

The second common mistake is **anemic services**: services that exist but only forward calls to the DbContext without adding any value. Look at the original `OwnerService.Create` from earlier in development:

```csharp
public Owner Create(Owner entity)
{
    _context.owners.Add(entity);
    _context.SaveChanges();
    return entity;
}
```

That's just a forwarder. There's no validation, no error translation, no business rule. If every service looks like this, the layer is decorative — you might as well delete it.

The current `OwnerService.Create` is better because it wraps `SaveChanges` in `try-catch` and translates `DbUpdateException` to `InvalidOperationException`. That's a real responsibility.

### Improvements for production

For a production app this size, you'd consider three additional concepts:

**Repository pattern.** A `IOwnerRepository` interface with `Add`, `Remove`, `Find`, etc. The service depends on the repository, not the DbContext directly. Pros: easier to mock for tests. Cons: another layer, more boilerplate. For most CRUD apps, EF Core's DbContext already plays the role of a repository, so adding another one is over-engineering.

**Mediator pattern (MediatR library).** Instead of injecting `OwnerService` everywhere, the controller dispatches a `CreateOwnerCommand` and a handler picks it up. Useful in larger systems where you have many services. Overkill here.

**DTOs (Data Transfer Objects).** The service currently accepts and returns the `Owner` entity. The view also receives the `Owner` entity. This couples the database schema to the UI — if you rename `Phone` to `PhoneNumber`, every layer breaks. A DTO like `OwnerCreateDto` decouples them: the view binds to the DTO, the controller maps the DTO to the entity, the service operates on the entity. More files but more flexibility.

### Exercises

**1. Predict.** A junior dev adds this code to `OwnerController.Index`:

```csharp
public async Task<IActionResult> Index()
{
    var owners = await _context.owners.ToListAsync();
    return View(owners);
}
```

What architectural rule does this violate? What concrete problem does this cause when you add caching later?

<details>
<summary>Solution</summary>

It violates the dependency direction — the controller is querying the DbContext directly instead of going through `_service.GetAllAsync()`. The concrete problem: when you later add caching to `OwnerService.GetAllAsync`, this controller bypasses the cache, returning fresh data from the DB on every request and inconsistent results between pages.
</details>

**2. Diagnose.** A teammate tells you they added authentication checks to every action of every controller (50+ checks). It's slow to maintain and they keep forgetting some. Where should the check actually live?

<details>
<summary>Solution</summary>

In a middleware or an `[Authorize]` attribute applied at the controller level, not in each action. ASP.NET Core has `app.UseAuthorization()` middleware specifically for this. If the rule is "all actions require login," apply `[Authorize]` to the base `Controller` or use a global filter. The pattern of repeating cross-cutting checks in every method is exactly what middleware solves.
</details>

**3. Implement.** Sketch the file structure (folder names + file names) you would create if you wanted to split this project into:
- A web project for the MVC views
- A separate API project for a future mobile app
- A shared class library for the services and entities

<details>
<summary>Solution</summary>

```
VetCare.sln
├── VetCare.Domain/        (class library)
│   ├── Models/            (Owner.cs, Pet.cs, ...)
│   └── Interfaces/        (ICrudService.cs, IEmailService.cs)
├── VetCare.Infrastructure/    (class library)
│   ├── Data/              (MysqlDbcontext.cs, Migrations/)
│   └── Services/          (OwnerService.cs, EmailService.cs, ...)
├── VetCare.Web/           (ASP.NET Core MVC project)
│   ├── Controllers/       (OwnerController.cs, ...)
│   ├── Views/             (cshtml files)
│   └── Program.cs
└── VetCare.Api/           (ASP.NET Core Web API project)
    ├── Controllers/       (OwnersController.cs, ...)
    └── Program.cs
```

Both `Web` and `Api` reference `Infrastructure`, which references `Domain`. The services and entities exist exactly once.
</details>

---

## 3. Models and Entities

### What it is

A model (or entity) is a C# class that represents a row in a database table. EF Core maps the class to the table automatically using conventions (class name → table name, property name → column name) plus any explicit configuration.

This project has 7 entities representing real-world concepts and 2 supporting classes:

- **Domain entities**: `Owner`, `Pet`, `Veterinary`, `Medicine`, `Consultation`, `Treatment`, `TreatmentMedicine`
- **Supporting**: `EmailSettings` (config POCO, not persisted), `ErrorViewModel` (for error pages)

### How it's used in this project

#### Owner

```csharp
public class Owner
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public List<Pet> Pets { get; set; } = [];
}
```

`Models/Owner.cs`

`Id` is the primary key. EF Core recognizes it by convention (`Id` or `<EntityName>Id`).

`Name` and `Phone` are required strings. The `= string.Empty` initializer prevents nullable warnings — without it, the compiler complains that a non-nullable string might be null at construction time.

`Pets` is a navigation collection. It represents "an owner has many pets." Notice it's typed as `List<Pet>`, not `IEnumerable<Pet>` — this matters and is explained in the bug catalog.

#### Pet

```csharp
public enum Species { Cat, Dog, Other }

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
```

`Models/Pet.cs`

`IdOwner` is the foreign key. `Owner` is the navigation property — the C# object reference to the actual `Owner` entity. The `?` (nullable) is critical: EF Core fills the navigation property only when you explicitly include it via `.Include(p => p.Owner)`. Without nullable, `ModelState.IsValid` would fail every time the form posts (you'd be telling MVC "every Pet must have an Owner object posted in the form" but the form only posts the FK integer).

`Species` is an enum stored in the database as an integer (0, 1, 2). Enums are a clean way to represent fixed sets of categories.

#### Veterinary

```csharp
public class Veterinary
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Speciality { get; set; } = string.Empty;
}
```

`Models/Veterinary.cs`

Minimal — just an Id, Name, and Speciality. The original requirements included a schedule, which was scoped out of the project.

#### Medicine

```csharp
public class Medicine
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Stock { get; set; }
}
```

`Models/Medicine.cs`

Stock is the inventory count. Used by reports and would be decremented in a full implementation when associated with a treatment.

#### Consultation

```csharp
public enum Status { Scheduled, Finished, Canceled, NoShow }

public class Consultation
{
    public int Id { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int IdPet { get; set; }
    public Pet? Pet { get; set; }
    public int IdVeterinary { get; set; }
    public Veterinary? Veterinary { get; set; }
    public DateTime DateStart { get; set; }
    public DateTime DateEnd { get; set; }
    public Status Status { get; set; }
}
```

`Models/Consultation.cs`

This is the central transactional entity. It links a `Pet` to a `Veterinary` at a specific time slot.

`Status` is a state machine: `Scheduled → Finished` (happy path), `Scheduled → Canceled` (user cancels), `Scheduled → NoShow` (pet didn't show up). `Finished` and `Canceled` and `NoShow` are terminal — once there, the consultation is closed.

The order of values in the enum matters for database storage. `Scheduled = 0`, `Finished = 1`, `Canceled = 2`, `NoShow = 3`. If you ever reorder them, all existing rows get the wrong status. Add new values at the end only.

#### Treatment

```csharp
public class Treatment
{
    public int Id { get; set; }
    public int IdConsultation { get; set; }
    public Consultation? Consultation { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime CreateAt { get; set; } = DateTime.UtcNow;
}
```

`Models/Treatment.cs`

A treatment belongs to a consultation. The business rule is that you can only register a treatment for a `Finished` consultation — that rule is enforced in `TreatmentService.Create`.

`CreateAt` defaults to `DateTime.UtcNow` at object construction time. For audit trails, this is fine. In a production system you'd want EF Core to set this on insert via `HasDefaultValueSql("CURRENT_TIMESTAMP")` so the DB clock is the source of truth.

#### TreatmentMedicine

```csharp
public class TreatmentMedicine
{
    public int Id { get; set; }
    public int IdMedicine { get; set; }
    public Medicine? Medicine { get; set; }
    public int IdTreatment { get; set; }
    public Treatment? Treatment { get; set; }
}
```

`Models/TreatmentMedicine.cs`

This is a **junction table** for the many-to-many relationship between `Treatment` and `Medicine`. A treatment can use multiple medicines. A medicine can be used in multiple treatments. The junction table has its own primary key (`Id`) plus the two foreign keys.

EF Core can also model many-to-many implicitly (no junction class), but having an explicit class lets you add fields like `Dose` or `Frequency` later.

#### EmailSettings

```csharp
public class EmailSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ClinicEmail { get; set; } = string.Empty;
}
```

`Models/EmailSettings.cs`

This is **not a database entity** — it's a configuration POCO. It's bound from `appsettings.json` in `Program.cs`:

```csharp
var emailSettings = builder.Configuration.GetSection("EmailSettings").Get<EmailSettings>()!;
builder.Services.AddSingleton(emailSettings);
```

The `Get<EmailSettings>()` reads the JSON section and creates an instance. Registered as Singleton because the configuration doesn't change at runtime.

### Why this approach

#### Why separate IdX and X for foreign keys

You'll notice every relationship has both `IdOwner` (the integer FK) and `Owner` (the navigation property). Why both?

Because EF Core needs the FK to write the database INSERT, but you as a developer want to navigate the relationship in code. Without the navigation property, you'd write:

```csharp
var pet = await _context.pets.FindAsync(petId);
var owner = await _context.owners.FindAsync(pet.IdOwner);
Console.WriteLine(owner.Name);
```

With it:

```csharp
var pet = await _context.pets.Include(p => p.Owner).FindAsync(petId);
Console.WriteLine(pet.Owner.Name);
```

Less code, fewer mistakes. The cost is you need to remember `Include()` to populate the navigation; otherwise it'll be null.

#### Why nullable navigation properties

Pet has `public Owner? Owner` (with `?`). This says "the Owner reference might or might not be loaded."

It's accurate because:
1. When you call `_context.pets.FindAsync(1)` without `Include`, the `Owner` navigation will indeed be null
2. When MVC posts the Pet form, only `IdOwner` is in the form data, so the `Owner` object is not constructed by model binding
3. When you create a new Pet in code and only set `IdOwner`, the `Owner` is null until EF Core's tracking populates it

If `Owner` were non-nullable, MVC would silently fail every form post because `ModelState` would mark `Owner` as required and the form wouldn't supply it.

#### Why = string.Empty initializers

C# 8 introduced **nullable reference types**. With `<Nullable>enable</Nullable>` in the csproj, every reference type is non-nullable by default unless marked with `?`.

`public string Name { get; set; }` — the compiler warns: "Name might be null after construction." Three ways to fix:

1. `public string Name { get; set; } = string.Empty;` — initialize to empty
2. `public required string Name { get; set; }` — C# 11+ keyword that forces callers to set it
3. `public string? Name { get; set; }` — declare nullable

For DB entities, option 1 is most common because EF Core constructs entities via reflection without going through your constructors, so option 2 doesn't always work. Option 3 changes the meaning (now Name *can* be null in the DB).

### Common mistakes

**Forgetting `= []` on collection navigation.** If you write `public List<Pet> Pets { get; set; }` without an initializer, accessing `.Pets.Add(somePet)` on a freshly-constructed Owner throws `NullReferenceException`. Always initialize collection navigations to an empty list.

**Using IEnumerable for collection navigation.** `IEnumerable<Pet>` is read-only. EF Core can't add to it during materialization, so the query crashes. Use `List<Pet>` or `ICollection<Pet>`.

**Putting business logic in the model.** Resist the urge to add methods like `pet.IsOldEnoughForVaccine()` or `consultation.CanBeCancelled()`. These belong in services so they can be tested without instantiating an entity, and so the rules can change without rebuilding the database schema.

**Mismatching enum values across releases.** If version 1 has `Status { Scheduled, Finished, Canceled }` (0/1/2) and version 2 inserts `NoShow` between `Scheduled` and `Finished`, every existing row's status now means something different. Always append.

### Improvements for production

**Add data annotations for validation.**

```csharp
[Required, StringLength(100)]
public string Name { get; set; } = string.Empty;

[Required, Phone]
public string Phone { get; set; } = string.Empty;
```

These annotations are picked up by both EF Core (for column constraints) and MVC (for `ModelState.IsValid`). The current models don't use them, which means the form happily accepts a 50,000-character name.

**Add value objects for common types.** Phone numbers, addresses, money — these are concepts that deserve their own type. A `PhoneNumber` value object can validate format on construction. The current `string Phone` doesn't.

**Audit fields.** Every entity benefits from `CreatedAt`, `UpdatedAt`, `CreatedBy`, `UpdatedBy`. The current `Treatment.CreateAt` is a half-step in this direction. A base `AuditableEntity` class can centralize them.

### Exercises

**1. Predict.** What does this code do?

```csharp
var owner = new Owner();
owner.Pets.Add(new Pet { Name = "Rex" });
```

Trace through what happens at each line. What if `Pets` had been declared without `= []`?

<details>
<summary>Solution</summary>

Line 1 creates an Owner with `Id = 0`, empty Name, empty Phone, and an empty `List<Pet>` because of the initializer.
Line 2 adds a Pet to that list. Without the `= []` initializer, `Pets` would be null and `owner.Pets.Add(...)` would throw `NullReferenceException`.
</details>

**2. Diagnose.** A teammate adds a new value `Rescheduled` to the `Status` enum, but inserts it between `Scheduled` and `Finished`:

```csharp
public enum Status { Scheduled, Rescheduled, Finished, Canceled, NoShow }
```

After deploying, the No-Show report shows wrong numbers and "finished" appointments are mysteriously appearing as "rescheduled." What happened?

<details>
<summary>Solution</summary>

The enum is stored in the DB as an integer. Before the change: `Finished = 1`. After the change: `Rescheduled = 1`, `Finished = 2`. All existing `Finished` rows in the DB still have value `1` — but `1` now maps to `Rescheduled`. New enum values must always be appended to preserve the integer mapping.
</details>

**3. Implement.** Add a new entity `Vaccine` with fields: `Id`, `Name`, `Manufacturer`, `Doses`. Then create a many-to-many relationship between `Pet` and `Vaccine` using a junction table `PetVaccine` with an additional `AdministeredAt` field. Sketch all three classes.

<details>
<summary>Solution</summary>

```csharp
public class Vaccine
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public int Doses { get; set; }
}

public class PetVaccine
{
    public int Id { get; set; }
    public int IdPet { get; set; }
    public Pet? Pet { get; set; }
    public int IdVaccine { get; set; }
    public Vaccine? Vaccine { get; set; }
    public DateTime AdministeredAt { get; set; }
}
```

Then add navigation in Pet: `public List<PetVaccine> PetVaccines { get; set; } = [];`
</details>

---

## 4. EF Core Deep Dive

### What it is

Entity Framework Core is the ORM (Object-Relational Mapper) that translates your C# classes into SQL queries. It's the bridge between the typed C# world and the SQL database.

The core concept is the **DbContext** — a class that represents a session with the database. You query through DbSets, modify entities, and call `SaveChanges` to commit.

### How it's used in this project

#### The DbContext

```csharp
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
```

`Data/MysqlDbcontext.cs`

The `DbSet<T>` properties are how you query each table. `_context.owners.ToListAsync()` returns all owners.

`OnModelCreating` is where you configure relationships using the **Fluent API**. Each `HasOne...WithMany().HasForeignKey()` call says: "Pet has one Owner, Owner has many Pets, the FK is `IdOwner`."

`WithMany(o => o.Pets)` says the inverse navigation is `Owner.Pets`. `WithMany()` (with no argument) means "the principal has many but I don't care to expose the collection on it."

#### Registration in Program.cs

```csharp
builder.Services.AddDbContext<MysqlDbcontext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("MysqlConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("MysqlConnection"))));
```

`Program.cs`

`AddDbContext` registers the DbContext with **Scoped** lifetime — one instance per HTTP request. This is critical: DbContext is *not* thread-safe. Two requests sharing a DbContext is a recipe for race conditions and duplicate inserts.

`UseMySql` tells EF Core to use the Pomelo MySQL provider. `AutoDetect` queries the server version to pick compatible SQL syntax (some MySQL functions vary between 5.7 and 8.0).

The connection string lives in `appsettings.json`:

```json
"ConnectionStrings": {
  "MysqlConnection": "Server=localhost;Database=simulation2;User=root;Password=password"
}
```

### Why this approach

#### Why Fluent API instead of Data Annotations

EF Core supports two ways to configure relationships:

**Data annotations** (attributes on the model):

```csharp
public class Pet
{
    [ForeignKey("Owner")]
    public int IdOwner { get; set; }
    public Owner Owner { get; set; }
}
```

**Fluent API** (in OnModelCreating):

```csharp
modelBuilder.Entity<Pet>()
    .HasOne(p => p.Owner)
    .HasForeignKey(p => p.IdOwner);
```

This project uses Fluent API for one reason: **the FK property name doesn't match EF Core's convention.**

The convention is: for navigation property `Owner`, the FK is `OwnerId` (NavigationName + "Id"). The project uses `IdOwner` ("Id" + EntityName), reversing the order.

Without explicit configuration, EF Core sees `IdOwner` and doesn't recognize it as the FK. It creates a *shadow property* `OwnerId` to use as the actual FK, leaving your `IdOwner` as a regular column. Inserts then fail because `OwnerId` is null. This is the convention conflict bug — covered in detail in the bug catalog.

`HasForeignKey(p => p.IdOwner)` tells EF Core: "Use this property, ignore the convention."

You could fix this by either:
1. Renaming `IdOwner` to `OwnerId` everywhere (changes Spanish naming style)
2. Adding `[ForeignKey("Owner")]` on the property
3. Configuring in Fluent API

Option 3 was chosen because all the configuration lives in one place (`OnModelCreating`) instead of scattered across model files.

#### Why eager loading with Include

EF Core does **not** automatically load navigation properties. This is intentional — implicit loading causes performance bugs.

When you write:

```csharp
var consultations = await _context.consultations.ToListAsync();
foreach (var c in consultations)
    Console.WriteLine(c.Pet.Name);  // c.Pet is null!
```

You expect `c.Pet` to be filled. It isn't. The query returned only the consultations, not the pets.

You have to explicitly include:

```csharp
var consultations = await _context.consultations
    .Include(c => c.Pet)
    .Include(c => c.Veterinary)
    .ToListAsync();
```

Now the SQL is a JOIN that returns consultations, pets, and vets in one round trip.

For nested navigation (consultation → treatment → medicine), use `ThenInclude`:

```csharp
await _context.treatments
    .Include(t => t.Consultation)
        .ThenInclude(c => c.Pet)
    .ToListAsync();
```

`TreatmentService.cs`

#### FindAsync vs FirstOrDefaultAsync

The project uses both. They look similar but behave differently.

**`FindAsync(id)`**:
- Looks up by primary key
- Checks the change tracker first — if the entity was already loaded in this DbContext, returns it without hitting the DB
- Cannot be combined with `Include`
- Returns null if not found

**`FirstOrDefaultAsync(predicate)`**:
- Always hits the DB
- Can be combined with `Include`
- Takes a lambda for any query, not just primary key

The pattern in this project:
- Use `FindAsync` when you don't need related data: `var owner = await _context.owners.FindAsync(id);`
- Use `FirstOrDefaultAsync` when you need includes: `var consultation = await _context.consultations.Include(c => c.Pet).FirstOrDefaultAsync(c => c.Id == id);`

### Common mistakes

**N+1 query problem.** Loading parent entities and iterating to load each child:

```csharp
var owners = await _context.owners.ToListAsync();
foreach (var o in owners)
{
    var pets = await _context.pets.Where(p => p.IdOwner == o.Id).ToListAsync();
    // ... do something
}
```

This issues 1 query for owners + N queries for each owner's pets. With 100 owners, that's 101 queries. The fix is `_context.owners.Include(o => o.Pets).ToListAsync()` — one query.

**Forgetting AsNoTracking for read-only queries.** EF Core tracks every entity it returns from a query so it can detect changes. For read-only operations (lists, reports), tracking is wasted work. Use `_context.owners.AsNoTracking().ToListAsync()` to skip it.

**Modifying entities outside the DbContext that loaded them.** If you load an Owner from one DbContext and try to update it through a different one, EF Core has no idea about the entity's tracking state. Either reattach it or reload from the new context.

**Not disposing the DbContext.** When using DI, ASP.NET Core handles disposal automatically. If you're using `new MysqlDbcontext()` somewhere (which you shouldn't), wrap it in `using`.

### Improvements for production

**Add database indexes.** The project's foreign keys have automatic indexes. But common search columns don't. For example, if reports group by `Veterinary.Name`, an index on that column speeds up groupings.

```csharp
modelBuilder.Entity<Veterinary>()
    .HasIndex(v => v.Name);
```

**Add unique constraints.** If a clinic shouldn't have two veterinaries with the exact same name and speciality:

```csharp
modelBuilder.Entity<Veterinary>()
    .HasIndex(v => new { v.Name, v.Speciality })
    .IsUnique();
```

**Use compiled queries for hot paths.** If the same query runs thousands of times per minute, EF Core's `EF.CompileQuery` skips the LINQ-to-SQL translation each time.

**Health checks.** Wire `app.MapHealthChecks("/health")` so a load balancer can detect when the DB connection is broken.

**Connection resilience.** `UseMySql(...).EnableRetryOnFailure()` retries transient errors (network blip, DB restart) automatically.

### Exercises

**1. Predict.** What SQL does this LINQ generate?

```csharp
var pets = await _context.pets
    .Include(p => p.Owner)
    .Where(p => p.Age > 5)
    .OrderBy(p => p.Name)
    .ToListAsync();
```

<details>
<summary>Solution</summary>

Approximately:

```sql
SELECT p.*, o.*
FROM pets p
LEFT JOIN owners o ON o.Id = p.IdOwner
WHERE p.Age > 5
ORDER BY p.Name
```

LEFT JOIN because `Owner?` is nullable. WHERE because of the `Where`. ORDER BY because of `OrderBy`. The Include translates to the JOIN.
</details>

**2. Diagnose.** This code returns owners but their `Pets` list is always empty:

```csharp
var owners = await _context.owners.ToListAsync();
return owners;
```

What's missing?

<details>
<summary>Solution</summary>

`.Include(o => o.Pets)`. Without it, EF Core doesn't load the children. Add it:

```csharp
var owners = await _context.owners.Include(o => o.Pets).ToListAsync();
```
</details>

**3. Implement.** Write the EF Core query that returns the top 3 most-used medicines, ordered by usage count, including the medicine name.

<details>
<summary>Solution</summary>

```csharp
var topMedicines = await _context.treatmentsMedicines
    .Include(tm => tm.Medicine)
    .GroupBy(tm => tm.Medicine!.Name)
    .Select(g => new { Name = g.Key, Count = g.Count() })
    .OrderByDescending(x => x.Count)
    .Take(3)
    .ToListAsync();
```

Note: in production you'd want to handle the null `Medicine` more safely. The actual project loads everything to memory first to avoid LINQ-to-Entities translation issues with the `??` operator on navigation properties.
</details>

---

## 5. Migrations

### What it is

A migration is a snapshot of your DbContext model at a point in time, expressed as code that can transform the database schema. EF Core compares the current model to the latest snapshot and generates the SQL needed to bring the database in sync.

Migrations are stored as `.cs` files in the `Migrations/` folder. Each migration has:
- An `Up` method — applies the change
- A `Down` method — reverts the change
- A timestamp prefix so migrations apply in order

### How it's used in this project

The project has 4 migrations:

| Timestamp | Name | Purpose |
|---|---|---|
| `20260426194954` | `FirstMigration` | Sets MySQL charset to utf8mb4 |
| `20260426195300` | `SecondMigration` | Creates all tables |
| `20260426211709` | `ConventionFix` | Renames `Medicines` → `medicines` |
| (next) | `AddNoShowStatus` (after running `dotnet ef migrations add`) | Adds NoShow value, fixes FK schema |

#### Reading a migration

A migration's `Up` method calls methods on `MigrationBuilder` to issue DDL:

```csharp
migrationBuilder.CreateTable(
    name: "owners",
    columns: table => new
    {
        Id = table.Column<int>(type: "int", nullable: false)
            .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
        Name = table.Column<string>(type: "longtext", nullable: false)
            .Annotation("MySql:CharSet", "utf8mb4"),
        Phone = table.Column<string>(type: "longtext", nullable: false)
            .Annotation("MySql:CharSet", "utf8mb4")
    },
    constraints: table =>
    {
        table.PrimaryKey("PK_owners", x => x.Id);
    })
    .Annotation("MySql:CharSet", "utf8mb4");
```

That's the equivalent of:

```sql
CREATE TABLE owners (
    Id INT AUTO_INCREMENT NOT NULL,
    Name LONGTEXT NOT NULL,
    Phone LONGTEXT NOT NULL,
    PRIMARY KEY (Id)
) CHARACTER SET utf8mb4;
```

#### Running migrations

Two commands matter:

```bash
dotnet ef migrations add MigrationName
```

Compares the current model to the last snapshot and generates a new migration `.cs` file describing the diff. **Doesn't touch the database.**

```bash
dotnet ef database update
```

Applies all pending migrations to the actual database.

If you mess up a migration before applying it, delete the .cs file and re-run `add`. After applying, you have to write a corrective migration.

### Why this approach

#### Why migrations instead of manual SQL

For a single-developer hobby project, you could run `CREATE TABLE` scripts manually. For a team project, you can't:

- Each developer has their own local DB
- Production DB exists somewhere else
- New devs joining need to spin up their environment

Migrations solve all three. Anyone running `dotnet ef database update` against a fresh DB ends up with the same schema. The migrations are committed to git, so the history is shared.

#### Why ConventionFix exists

Earlier in development, EF Core's convention named the medicines table `Medicines` (PascalCase). MySQL on Linux is case-sensitive, so `medicines` and `Medicines` are different tables. The fix-up migration renamed it to lowercase to match the rest of the schema.

This is a common gotcha when developing on Windows MySQL (case-insensitive) and deploying to Linux MySQL (case-sensitive).

### Common mistakes

**Editing applied migrations.** Once a migration has been applied to any database, never edit its `.cs` file. The migrations table records what's been applied by name; if you change `Up` after applying, the change won't run again. Write a corrective migration instead.

**Forgetting to commit the migration to git.** A migration file generated locally must be committed. If you forget, your teammates' databases will diverge from yours.

**Manually running ALTER TABLE.** Bypassing migrations to hand-tweak the DB schema means the next migration will detect a "drift" and try to undo your manual changes. Always go through migrations.

**Accumulating tons of small migrations during dev.** Before merging to main, squash trivial migrations into one logical migration ("InitialSchema"). Avoid 50 migrations named "fix1", "fix2", "fix3" cluttering the history.

### Improvements for production

**Backup before every migration.** Production migrations can fail mid-way. A backup is your only insurance.

**Migrations as part of CI/CD.** Don't run `dotnet ef database update` from a developer machine against production. Have a deploy pipeline that runs it as a documented step.

**Idempotent migrations.** EF Core can generate idempotent SQL with `dotnet ef migrations script --idempotent`. The output script can be run multiple times safely.

**Avoid data migrations in schema migrations.** If you need to backfill data ("set Status to NoShow for all Scheduled in the past"), prefer a separate script over mixing it into a schema migration.

### Exercises

**1. Predict.** You added a new property `Email` to the `Owner` model. You ran `dotnet ef migrations add AddOwnerEmail`. Then you ran the app — it crashes saying the column doesn't exist. Why?

<details>
<summary>Solution</summary>

You added the migration but didn't apply it. `dotnet ef migrations add` only generates the migration code. You must run `dotnet ef database update` to actually alter the database.
</details>

**2. Diagnose.** You see a migration file `20240101_AddPhoneToOwner.cs` in the project. The team says it was applied weeks ago in production. A junior dev edits the `Up` method to also add an index. They commit and deploy. The index doesn't appear in production. Why?

<details>
<summary>Solution</summary>

Editing already-applied migrations doesn't re-run them. The `__EFMigrationsHistory` table records that `20240101_AddPhoneToOwner` was applied — EF Core considers it done. The fix is to write a new migration `AddIndexToOwnerPhone` that adds the index, and never edit applied migrations again.
</details>

**3. Implement.** Write the migration code (in `Up`) that would add a `Stock` column to `medicines` with default value `0`.

<details>
<summary>Solution</summary>

```csharp
migrationBuilder.AddColumn<int>(
    name: "Stock",
    table: "medicines",
    type: "int",
    nullable: false,
    defaultValue: 0);
```

You'd also need a corresponding `Down` that removes it:

```csharp
migrationBuilder.DropColumn(
    name: "Stock",
    table: "medicines");
```

In practice, you'd add the property to the `Medicine` model first and let `dotnet ef migrations add` generate this code for you.
</details>

---

## 6. The Service Layer and ICrudService

### What it is

A service is a class that contains business logic. It sits between the controller (HTTP) and the DbContext (persistence). The interface defines a contract; the implementation does the work.

### How it's used in this project

#### The interface

```csharp
public interface ICrudService<T> where T : class
{
    T Create(T entity);
    Task<IEnumerable<T>> GetAllAsync();
    Task<T> GetByIdAsync(int id);
    Task<T> UpdateAsync(T entity);
    Task<T> DeleteAsync(int id);
}
```

`Interfaces/ICrudService.cs`

A generic CRUD interface. Any service that handles a basic entity can implement this contract: 5 methods covering Create, Read All, Read One, Update, Delete.

The `where T : class` constraint says T must be a reference type — required because `FindAsync(id)` returns `null` if not found, and value types can't be null.

#### A complete implementation

```csharp
public class OwnerService : ICrudService<Owner>
{
    private readonly MysqlDbcontext _context;

    public OwnerService(MysqlDbcontext context)
    {
        _context = context;
    }

    public Owner Create(Owner entity)
    {
        try
        {
            _context.owners.Add(entity);
            _context.SaveChanges();
            return entity;
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("Could not save owner.", ex);
        }
    }

    public async Task<IEnumerable<Owner>> GetAllAsync()
    {
        try
        {
            return await _context.owners.ToListAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Could not retrieve owners.", ex);
        }
    }

    public async Task<Owner> GetByIdAsync(int id)
    {
        try
        {
            var owner = await _context.owners.FindAsync(id)
                ?? throw new KeyNotFoundException($"Owner with id {id} not found.");
            return owner;
        }
        catch (KeyNotFoundException) { throw; }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Could not retrieve owner.", ex);
        }
    }

    public async Task<Owner> UpdateAsync(Owner entity)
    {
        try
        {
            var existing = await _context.owners.FindAsync(entity.Id)
                ?? throw new KeyNotFoundException($"Owner with id {entity.Id} not found.");

            existing.Name = entity.Name;
            existing.Phone = entity.Phone;

            await _context.SaveChangesAsync();
            return existing;
        }
        catch (KeyNotFoundException) { throw; }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("Could not update owner.", ex);
        }
    }

    public async Task<Owner> DeleteAsync(int id)
    {
        try
        {
            var owner = await _context.owners.FindAsync(id)
                ?? throw new KeyNotFoundException($"Owner with id {id} not found.");

            _context.owners.Remove(owner);
            await _context.SaveChangesAsync();
            return owner;
        }
        catch (KeyNotFoundException) { throw; }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("Could not delete owner.", ex);
        }
    }
}
```

`Services/OwnerService.cs`

Each method follows the same shape:

1. Try the operation
2. Catch infrastructure exceptions (DbUpdateException, generic Exception)
3. Translate to a domain exception (InvalidOperationException) with a meaningful message
4. Re-throw KeyNotFoundException as-is (it's already a domain exception)

#### Constructor injection

The service depends on `MysqlDbcontext`. It receives the dependency through the constructor:

```csharp
private readonly MysqlDbcontext _context;

public OwnerService(MysqlDbcontext context)
{
    _context = context;
}
```

This is **constructor injection**. The DI container handles wiring — when MVC needs an `OwnerService`, it sees the constructor needs a `MysqlDbcontext`, finds the registration in `Program.cs`, creates an instance, and passes it in.

The `readonly` keyword means `_context` can only be assigned once (in the constructor). It enforces immutability of the dependency.

### Why this approach

#### Why a generic interface

Without `ICrudService<T>`, you'd write 5 separate interfaces:

```csharp
public interface IOwnerService { ... }
public interface IPetService { ... }
public interface IVeterinaryService { ... }
public interface IMedicineService { ... }
public interface IConsultationService { ... }
```

Each with the same 5 method signatures. Boilerplate.

The generic interface DRYs it up. One contract, six implementations.

**The trade-off** is that it forces every service into the same shape. `ConsultationService.Create` needs business rules — past date check, overlap detection, no-show block. It still implements `Create(Consultation entity)` from the interface, but the body is much longer than `OwnerService.Create`. The interface doesn't expose the difference.

For a senior engineer on a complex domain, this is where the abstraction breaks down and you'd reach for **specific** interfaces per service:

```csharp
public interface IConsultationService
{
    Consultation Create(Consultation entity);
    Task<bool> CanScheduleAsync(int petId, int vetId, DateTime start, DateTime end);
    Task<int> CountActiveForPetAsync(int petId);
    // ... etc
}
```

Each method shaped to the actual domain operation. More files but clearer intent.

#### Why try-catch in services

Without try-catch, an `DbUpdateException` from EF Core bubbles all the way up to the MVC pipeline. The user sees a 500 error page. The error message exposes implementation details ("Duplicate entry for key X").

With try-catch + translation, the controller catches a clean `InvalidOperationException` with a domain-meaningful message ("Could not save owner. Please try again.").

The pattern is **exception translation**:

| Layer | Exception type |
|---|---|
| EF Core | `DbUpdateException`, `DbUpdateConcurrencyException` |
| Service | Translates to `InvalidOperationException`, `KeyNotFoundException` |
| Controller | Translates to HTTP status (`NotFound`, `BadRequest`, `View`) |

#### Why some methods are sync and others async

`Create` is synchronous (`Owner Create(Owner entity)`). `GetAllAsync` is async (`Task<IEnumerable<Owner>> GetAllAsync()`).

This is **inconsistent** and arguably a flaw in the original interface design. The reason it works is that ASP.NET Core can call sync methods from async actions without issue.

In an ideal world, every I/O operation (DB calls) is async. The interface would be:

```csharp
public interface ICrudService<T> where T : class
{
    Task<T> CreateAsync(T entity);
    Task<IEnumerable<T>> GetAllAsync();
    Task<T> GetByIdAsync(int id);
    Task<T> UpdateAsync(T entity);
    Task<T> DeleteAsync(int id);
}
```

Refactoring to all-async is a good exercise.

### Common mistakes

**Putting database access in the controller.** As covered in section 2, the controller should call the service, not the DbContext directly.

**Catching `Exception` and swallowing it.**

```csharp
catch (Exception ex) { /* nothing */ }
```

The bug disappears silently. The user sees success, but no record was saved. Always either re-throw, translate, or log.

**Throwing in finally blocks.** A `throw` inside `finally` masks any exception that was already in flight. Just don't.

**Mixing concerns inside a service method.** A `Create` method that also sends emails, generates PDFs, and updates a search index does too much. Each of those should be its own service or background job.

### Improvements for production

**Add a base class for shared logic.**

```csharp
public abstract class BaseCrudService<T> : ICrudService<T> where T : class
{
    protected readonly MysqlDbcontext _context;
    public BaseCrudService(MysqlDbcontext context) { _context = context; }
    
    public virtual async Task<T> GetByIdAsync(int id)
    {
        // shared GetByIdAsync logic
    }
}
```

`OwnerService` extends `BaseCrudService<Owner>` and only overrides what differs.

**Inject ILogger for diagnostics.** Currently, exceptions are translated but never logged. In production, you want a record of every error that happened.

```csharp
public OwnerService(MysqlDbcontext context, ILogger<OwnerService> logger) { ... }

catch (DbUpdateException ex)
{
    _logger.LogError(ex, "Failed to save owner {OwnerId}", entity.Id);
    throw new InvalidOperationException("Could not save owner.", ex);
}
```

**Result types instead of exceptions.** For expected failure cases (validation errors, not-found), some teams prefer returning a `Result<T>` type:

```csharp
public Result<Owner> Create(Owner entity)
{
    if (!IsValid(entity))
        return Result.Failure<Owner>("Invalid data");
    ...
    return Result.Success(entity);
}
```

Pros: explicit, no surprises. Cons: more boilerplate, every caller must check.

### Exercises

**1. Predict.** A user calls `await _service.GetByIdAsync(99999)` for an ID that doesn't exist. Trace what happens — which exception fires, where it's caught, what HTTP response the user gets.

<details>
<summary>Solution</summary>

`FindAsync(99999)` returns null. The `?? throw new KeyNotFoundException(...)` fires. The first `catch (KeyNotFoundException) { throw; }` lets it propagate. The controller catches it: `catch (KeyNotFoundException) { return NotFound(); }`. The user sees an HTTP 404 response.
</details>

**2. Diagnose.** A teammate writes:

```csharp
public async Task<Owner> UpdateAsync(Owner entity)
{
    try
    {
        _context.owners.Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }
    catch (Exception ex)
    {
        return null;
    }
}
```

Two problems. Find them.

<details>
<summary>Solution</summary>

1. Catching `Exception` and returning `null` swallows all errors silently. The caller has no way to know what went wrong.
2. `_context.owners.Update(entity)` directly attaches the passed-in entity to the context. If `entity.Id` doesn't exist in the DB, this either inserts a new row (with the wrong Id) or fails opaquely. The original pattern using `FindAsync(entity.Id)` first ensures the record exists and only mutates allowed fields.
</details>

**3. Implement.** Add a method `CountActiveAsync()` to `ConsultationService` that returns the number of consultations with `Status.Scheduled`. Decide whether to add it to the interface or only to the concrete class.

<details>
<summary>Solution</summary>

Add it to the concrete class only. `ICrudService<T>` is a generic CRUD contract; counting active consultations is a Consultation-specific concern. Adding it to the interface forces every CRUD service to implement irrelevant methods.

```csharp
public async Task<int> CountActiveAsync()
{
    return await _context.consultations.CountAsync(c => c.Status == Status.Scheduled);
}
```

To call it, inject `ConsultationService` (the concrete type) wherever you need it.
</details>

---

## 7. Controllers and HTTP Handling

### What it is

A controller is a class that handles HTTP requests. Each method (action) corresponds to a URL pattern. The controller's job is to:

1. Accept the request (path, query string, form data, body)
2. Validate the input
3. Call services to do the work
4. Return an HTTP response (HTML view, JSON, redirect, status code)

### How it's used in this project

The project has 8 controllers, all in `Controllers/`:

- `HomeController` — landing page and privacy
- `OwnerController` — CRUD for owners
- `PetController` — CRUD for pets + medical history
- `VeterinaryController` — CRUD for vets
- `MedicineController` — CRUD for medicines
- `ConsultationController` — CRUD for appointments
- `TreatmentController` — CRUD for treatments
- `ReportController` — aggregate reports

#### Anatomy of a controller action

```csharp
[HttpGet]
public async Task<IActionResult> Update(int id)
{
    try
    {
        var owner = await _service.GetByIdAsync(id);
        return View(owner);
    }
    catch (KeyNotFoundException)
    {
        return NotFound();
    }
}
```

`OwnerController.cs`

- `[HttpGet]` — only responds to GET requests
- `Update(int id)` — the action name and parameters; `id` is bound from the URL via routing
- `IActionResult` — the response type; can be a View, Redirect, NotFound, etc.
- `_service.GetByIdAsync(id)` — delegates to the service
- `View(owner)` — renders `Views/Owner/Update.cshtml` with `owner` as the model
- `NotFound()` — returns HTTP 404

#### The Update flow (GET + POST pair)

Every form-driven update has two actions:

```csharp
[HttpGet]
public async Task<IActionResult> Update(int id) { ... }    // shows the form

[HttpPost]
public async Task<IActionResult> Update(Owner owner) { ... }  // processes the form
```

GET responds to "show me the form for owner #5." POST responds to "user submitted the form."

These are two separate methods with the same name but different parameters. C# overloading lets you name them the same; routing selects based on the HTTP verb.

#### POST handling

```csharp
[HttpPost]
public async Task<IActionResult> Update(Owner owner)
{
    if (!ModelState.IsValid)
        return View(owner);

    try
    {
        await _service.UpdateAsync(owner);
        return RedirectToAction("Index");
    }
    catch (KeyNotFoundException)
    {
        return NotFound();
    }
    catch (InvalidOperationException ex)
    {
        ModelState.AddModelError(string.Empty, ex.Message);
        return View(owner);
    }
}
```

`OwnerController.cs`

The pattern:

1. Check `ModelState.IsValid` — if the form had validation errors, re-render the form (with the errors visible)
2. Call the service
3. On success, redirect to Index (PRG pattern)
4. On `KeyNotFoundException`, return 404
5. On domain failure, add the error to ModelState and re-render the form

The **PRG pattern** (POST-Redirect-GET) prevents the "are you sure you want to resubmit this form?" dialog when users hit refresh. After a successful POST, the response is a redirect to a GET URL.

#### Routing

Routing is configured in `Program.cs`:

```csharp
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
```

This says: a URL like `/Owner/Update/5` maps to `OwnerController.Update(int id)` with `id = 5`. Defaults: `controller = Home`, `action = Index`. So `/` maps to `HomeController.Index()`.

The `{id?}` means id is optional. `/Owner/Index` works without an id.

#### Dependency injection in controllers

```csharp
public class OwnerController : Controller
{
    private readonly OwnerService _service;

    public OwnerController(OwnerService service)
    {
        _service = service;
    }
}
```

Same constructor injection pattern as services. ASP.NET Core sees the constructor needs `OwnerService`, finds the registration, and provides it.

A controller can inject multiple services:

```csharp
public PetController(PetService service, OwnerService ownerService, MysqlDbcontext context)
{
    _service = service;
    _ownerService = ownerService;
    _context = context;
}
```

`PetController.cs`

PetController needs PetService for CRUD, OwnerService to populate the owner dropdown, and DbContext directly for the History query.

### Why this approach

#### Why ModelState.IsValid

Before your action runs, ASP.NET Core has already:

1. Read the form data from the HTTP request body
2. Bound it to your action's parameter (`Owner owner`)
3. Validated each property based on data annotations and model validation

`ModelState` is the bag of validation results. `IsValid` is true if no validations failed.

If you skip `ModelState.IsValid`, your service receives invalid data — empty strings, negative numbers, malformed dates. The service either accepts garbage (bad) or throws (good but ugly).

The pattern is:

```csharp
if (!ModelState.IsValid)
    return View(model);  // re-render with errors visible
```

The view's `<span asp-validation-for="Name">` tag helpers automatically show the validation errors next to each field.

#### Why Delete is HttpPost

A common beginner question: "why isn't Delete just a link?"

Two reasons:

1. **CSRF**. A malicious site could include `<img src="https://yoursite.com/Owner/Delete/5">` and the user's browser would send the request automatically. Browsers don't auto-submit POSTs from images.
2. **Browser prefetch**. Some browsers prefetch links to make navigation feel faster. If Delete were a GET, opening a list of owners would silently delete one.

The convention is: GET is safe (no side effects). Anything that modifies data uses POST/PUT/PATCH/DELETE.

The view uses a tiny inline form for the Delete button:

```html
<form asp-controller="Owner" asp-action="Delete" asp-route-id="@o.Id" method="post" style="display:inline">
    <button type="submit" class="btn btn-outline-danger">Delete</button>
</form>
```

`Views/Owner/Index.cshtml`

#### Why ViewBag for dropdowns

The Pet form needs an owner dropdown. The list of owners isn't part of the Pet model — it's auxiliary data needed by the view.

Three options to pass it:

1. **ViewModel** — create a `PetCreateViewModel { Pet Pet, IEnumerable<Owner> Owners }`
2. **ViewBag** — `ViewBag.Owners = await _ownerService.GetAllAsync();`
3. **ViewData** — string-keyed dictionary, like ViewBag but typed differently

This project uses ViewBag because the user requested simplicity. A ViewModel is more correct (typed, refactor-safe) but adds a class per controller.

The trade-off: ViewBag is dynamic, so typos in the view (`ViewBag.Onwers`) silently return null instead of compile errors. ViewModels catch typos at compile time.

### Common mistakes

**Forgetting `[HttpPost]`.** An action without `[HttpPost]` defaults to accepting both GET and POST. Combined with browser prefetch, this can cause unintended state changes.

**Returning the wrong View.** `return View()` (no argument) renders the view named after the action. `return View(model)` does the same but with a model. `return View("OtherView", model)` lets you specify a different view file. Easy to mix up.

**Mixing async and sync.** `_service.Create(owner)` (sync) inside an `async Task<IActionResult>` action works but isn't taking advantage of async I/O. Calling `_service.GetAllAsync().Result` (instead of `await _service.GetAllAsync()`) can deadlock in some configurations.

**Not validating route parameters.** If the URL is `/Owner/Update/abc`, model binding will fail and the action won't even run — but you might write code that assumes `id` is always a positive integer. ASP.NET Core handles non-integer cases automatically; explicit checks for `id <= 0` are still a good idea.

### Improvements for production

**Anti-forgery tokens on POST forms.**

```html
<form asp-controller="Owner" asp-action="Delete" asp-route-id="@o.Id" method="post">
    @Html.AntiForgeryToken()
    <button type="submit">Delete</button>
</form>
```

Combined with `[ValidateAntiForgeryToken]` on the action, this prevents CSRF attacks even when the user is logged in.

**Action filters for cross-cutting concerns.** If every action needs to log entry/exit, write an `[ActionFilter]` instead of repeating `_logger.LogInformation` everywhere.

**API versioning.** If you ever expose a JSON API, version it (`/api/v1/owners`) so changes don't break existing clients.

**Output caching.** For pages that don't change often (Index lists), `[OutputCache(Duration = 60)]` caches the response for a minute, drastically reducing DB load.

### Exercises

**1. Predict.** What HTTP status code does this return when `id = 99999` and there's no owner with that id?

```csharp
[HttpGet]
public async Task<IActionResult> Update(int id)
{
    try
    {
        var owner = await _service.GetByIdAsync(id);
        return View(owner);
    }
    catch (KeyNotFoundException)
    {
        return NotFound();
    }
}
```

<details>
<summary>Solution</summary>

HTTP 404. `GetByIdAsync` throws `KeyNotFoundException`, the controller catches it and returns `NotFound()`, which is shorthand for `new NotFoundResult()` — HTTP 404.
</details>

**2. Diagnose.** A user reports that creating an owner with a blank name is allowed. The Owner model has `[Required]` on Name. Why does it slip through?

<details>
<summary>Solution</summary>

The controller doesn't check `ModelState.IsValid` before calling the service. The data annotation only fills `ModelState` — it doesn't block the action. The fix is to add `if (!ModelState.IsValid) return View(owner);` at the top of the POST action.
</details>

**3. Implement.** Add a new action to `OwnerController` that returns owners filtered by name. URL pattern: `/Owner/Search/Juan`. Show only the controller code; don't worry about the view.

<details>
<summary>Solution</summary>

```csharp
[HttpGet]
public async Task<IActionResult> Search(string q)
{
    var all = await _service.GetAllAsync();
    var filtered = all.Where(o => o.Name.Contains(q ?? "", StringComparison.OrdinalIgnoreCase));
    return View("Index", filtered);
}
```

URL would be `/Owner/Search?q=Juan`. To make it `/Owner/Search/Juan`, you'd configure routing or use `[Route("Search/{q}")]` on the action.
</details>

---

## 8. Views with Razor

### What it is

Razor is the templating language for ASP.NET Core MVC views. A `.cshtml` file is HTML mixed with C# code. The Razor engine compiles it into a class that produces an HTML string.

### How it's used in this project

#### File organization

```
Views/
├── _ViewImports.cshtml    ← global usings + tag helpers
├── _ViewStart.cshtml       ← runs before every view
├── Shared/
│   ├── _Layout.cshtml      ← master template
│   ├── Error.cshtml
│   └── _ValidationScriptsPartial.cshtml
├── Home/
│   ├── Index.cshtml
│   └── Privacy.cshtml
├── Owner/
│   ├── Index.cshtml        ← list page
│   ├── Create.cshtml       ← create form
│   └── Update.cshtml       ← edit form
└── ... (one folder per controller)
```

The `Views/<ControllerName>/<ActionName>.cshtml` convention is automatic — `OwnerController.Index()` → `Views/Owner/Index.cshtml`.

#### _ViewImports

```cshtml
@using simulationTest
@using simulationTest.Models
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
```

`Views/_ViewImports.cshtml`

This adds `using` directives to every view (so you don't need `@using` at the top of each file) and enables tag helpers.

#### _ViewStart

```cshtml
@{
    Layout = "_Layout";
}
```

`Views/_ViewStart.cshtml`

Runs before every view and sets the default layout. Without this, each view would need `@{ Layout = "_Layout"; }` at the top.

#### _Layout

The master template. Defines the HTML skeleton, sidebar, header, and renders the per-view content via `@RenderBody()`.

```html
<!DOCTYPE html>
<html lang="en">
<head>
    <title>@ViewData["Title"] - VetCare</title>
    ...
</head>
<body>
    <aside class="vc-sidebar">
        ...navigation links...
    </aside>
    <div class="vc-shell">
        <header class="vc-header">@ViewData["Title"]</header>
        <div class="vc-content">
            <div class="vc-card">
                @RenderBody()
            </div>
        </div>
    </div>
</body>
</html>
```

`Views/Shared/_Layout.cshtml`

#### Tag helpers

Tag helpers extend HTML elements with server-side functionality. The most common in this project:

```html
<form asp-controller="Owner" asp-action="Create" method="post">
    <label asp-for="Name" class="form-label">Name</label>
    <input asp-for="Name" type="text" class="form-control">
    <span asp-validation-for="Name" class="text-danger"></span>
    <button class="btn btn-success">Create</button>
</form>
```

`Views/Owner/Create.cshtml`

- `asp-controller` + `asp-action` → generates the form's `action` URL via routing
- `asp-for` → binds the input to a model property; sets `name`, `id`, and adds validation attributes
- `asp-validation-for` → renders the validation error message for that field

The tag helpers turn this Razor:

```html
<input asp-for="Name" type="text">
```

into this HTML:

```html
<input type="text" id="Name" name="Name" data-val="true" data-val-required="The Name field is required." value="">
```

#### Razor expressions

Inline expressions: `@variableName`, `@object.Property`, `@(expression)`.

Code blocks: `@{ ... }` for statements.

Conditionals: `@if (condition) { ... }`.

Loops: `@foreach (var x in collection) { ... }`.

Example from the Pet Index:

```html
@model IEnumerable<Pet>

<table class="table table-striped">
    <tbody>
    @foreach (var p in Model)
    {
        <tr>
            <td>@p.Id</td>
            <td>@p.Name</td>
            <td>@p.Species</td>
            <td>@p.Owner?.Name</td>
        </tr>
    }
    </tbody>
</table>
```

`Views/Pet/Index.cshtml`

`@model IEnumerable<Pet>` declares the type the view receives. `Model` then refers to the typed object.

#### The hidden Id pattern

Every Update form has this:

```html
<input asp-for="Id" type="hidden">
```

`Views/Owner/Update.cshtml`

When the form is submitted, the `Id` value is included in the POST body. Without it, model binding sets `Id = 0` and the service's `FindAsync(0)` returns null, throwing `KeyNotFoundException`.

#### Dropdowns with foreach

The Pet Update view needs an owner dropdown that pre-selects the current owner:

```html
<select asp-for="IdOwner" class="form-select">
    @foreach (var owner in (IEnumerable<Owner>)ViewBag.Owners)
    {
        if (Model.IdOwner == owner.Id)
        {
            <option value="@owner.Id" selected>@owner.Name</option>
        }
        else
        {
            <option value="@owner.Id">@owner.Name</option>
        }
    }
</select>
```

`Views/Pet/Update.cshtml`

The `if/else` is needed because Razor doesn't allow C# expressions inside attribute values when the parent element is processed by a tag helper. `<option value="1" @(expr ? "selected" : "")>` doesn't compile.

### Why this approach

#### Why server-side rendering

This project does server-side rendering (the server returns HTML). The alternative is client-side rendering (the server returns JSON, the client renders HTML via JavaScript).

**Server-side pros:**
- Works without JavaScript
- Simpler architecture (no separate API)
- Faster initial page load (no JS bundle to download)
- SEO-friendly out of the box

**Server-side cons:**
- Page navigations cause full reloads
- Less interactive feel
- Harder to share state across pages

For a CRUD admin tool used by clinic staff, server-side is the right choice. For a customer-facing app with high interactivity (Twitter, Slack), client-side wins.

#### Why tag helpers over HTML helpers

Old MVC used HTML helpers: `@Html.LabelFor(m => m.Name)`. New Razor uses tag helpers: `<label asp-for="Name">`.

Tag helpers are better because:
- They look like HTML
- IDE autocomplete works for them like normal HTML
- Easier for designers to read

You can mix both, but the codebase is cleaner if you pick one and stick to it.

#### Why ViewBag instead of strongly-typed ViewModels

Trade-off discussed earlier. ViewBag wins on simplicity for small projects. ViewModels win on type safety for larger ones.

### Common mistakes

**Forgetting `@model`.** Without `@model Pet`, you can't write `@Model.Name` — the type is unknown.

**Using a model where Razor expects something else.** The Index view has `@model IEnumerable<Pet>`. The Create view has `@model Pet`. Mixing them up causes runtime exceptions.

**Casting ViewBag wrong.** `(IEnumerable<Owner>)ViewBag.Owners` works only if you actually set `ViewBag.Owners = somethingThatIsIEnumerableOfOwners`. A typo silently returns null.

**Forgetting to handle null navigation properties.** `@p.Owner.Name` crashes if Owner wasn't loaded. Use `@p.Owner?.Name` (null-conditional).

**Inline JavaScript in views.** Putting `<script>` blocks inside views works but doesn't scale. Move to external files for anything beyond trivial.

### Improvements for production

**Strongly-typed ViewModels.** Replace ViewBag with classes:

```csharp
public class PetCreateViewModel
{
    public Pet Pet { get; set; }
    public IEnumerable<Owner> Owners { get; set; } = [];
}
```

The view uses `@model PetCreateViewModel` and accesses `Model.Owners` directly, with full IntelliSense.

**Partial views for reused UI.** A pet card displayed in 3 different pages should be a `_PetCard.cshtml` partial included via `<partial name="_PetCard" model="@p" />`.

**View components for complex widgets.** A "user menu" with logic (count of unread notifications, current avatar) is better as a `ViewComponent` than a partial — it can have its own data fetching.

**Tag helpers for consistency.** Custom tag helpers can centralize repeated markup. A `<vetcare-table>` tag helper that generates a styled table eliminates copy-paste.

### Exercises

**1. Predict.** What does this view render?

```html
@model IEnumerable<Owner>

@foreach (var o in Model)
{
    <p>@o.Name (@o.Pets.Count pets)</p>
}
```

What if `o.Pets` is null?

<details>
<summary>Solution</summary>

For each owner, renders `<p>OwnerName (NumberOfPets pets)</p>`. If `o.Pets` is null, `o.Pets.Count` throws `NullReferenceException`. The fix is `@(o.Pets?.Count ?? 0)`.
</details>

**2. Diagnose.** A user reports the Update form for owners doesn't update — clicking Save just shows the form again. The validation messages aren't visible either. What's likely missing?

<details>
<summary>Solution</summary>

Most likely the form is missing `<input asp-for="Id" type="hidden">`. Without it, the POST sends `Id = 0`, the service throws `KeyNotFoundException`, and the controller returns NotFound (which would show 404, not the form again — but if NotFound was being caught somewhere or the exception bubbles to a different path, you'd see the form re-render).

A second possibility: `asp-validation-for` spans aren't on each field, so validation errors don't show. Check both.
</details>

**3. Implement.** Write a partial view `_OwnerSummary.cshtml` that takes an `Owner` model and renders the name and phone. Then write the line in another view that includes it.

<details>
<summary>Solution</summary>

```html
@* _OwnerSummary.cshtml *@
@model Owner

<div class="owner-summary">
    <strong>@Model.Name</strong>
    <span>@Model.Phone</span>
</div>
```

To include it elsewhere:

```html
<partial name="_OwnerSummary" model="@someOwner" />
```
</details>

---

## 9. Form Validation and ModelState

### What it is

Form validation is the process of checking that user-submitted data meets the rules before processing it. ASP.NET Core MVC has a built-in pipeline:

1. Form data arrives in the HTTP request
2. Model binder maps it to your action's parameter
3. Validators check each property
4. Validation results go into `ModelState`
5. Your action checks `ModelState.IsValid` and decides what to do

### How it's used in this project

#### Implicit Required from nullable reference types

The project enables `<Nullable>enable</Nullable>` in the csproj. This makes every reference type non-nullable by default.

```csharp
public string Name { get; set; } = string.Empty;
```

The compiler treats `Name` as never-null. MVC's validation system **inherits** this and adds an implicit `[Required]`. Submitting a form with empty Name marks `ModelState` invalid.

This is convenient but causes problems with navigation properties, which is why this project marks them nullable:

```csharp
public Owner? Owner { get; set; }
```

The `?` tells MVC: "this might be null, don't require it." Without the `?`, the form submission would always fail because the `Owner` complex object isn't part of the form data — only `IdOwner` is.

#### Where validation happens

Three places:

**1. The model.** Annotations describe the rules.

```csharp
public class Owner
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
}
```

`Models/Owner.cs`

This project doesn't currently have `[Required]`, `[StringLength]`, etc. — that's a gap. The implicit Required from non-nullable strings is the only enforcement.

**2. The controller.** Checks `ModelState.IsValid`.

```csharp
[HttpPost]
public IActionResult Create(Owner owner)
{
    if (!ModelState.IsValid)
        return View(owner);

    try
    {
        _service.Create(owner);
        return RedirectToAction("Index");
    }
    ...
}
```

`OwnerController.cs`

**3. The view.** Displays the errors.

```html
<input asp-for="Name" type="text" class="form-control">
<span asp-validation-for="Name" class="text-danger"></span>
```

`Views/Owner/Create.cshtml`

The `<span asp-validation-for>` tag helper renders the error message for the `Name` field.

`<div asp-validation-summary="All" class="text-danger mb-3"></div>` shows all errors in one block (used in Consultation forms).

### Why this approach

#### Why ModelState exists

Without ModelState, you'd write validation manually in every action:

```csharp
if (string.IsNullOrEmpty(owner.Name))
    return BadRequest("Name is required");
if (owner.Name.Length > 100)
    return BadRequest("Name too long");
if (!IsValidPhone(owner.Phone))
    return BadRequest("Invalid phone");
```

ModelState centralizes this. You declare rules once on the model; MVC enforces them automatically.

#### Why navigation properties cause failures

When MVC binds the POST body to your action parameter, it walks the model's properties. For each non-nullable reference type, it checks if the value was supplied.

The Pet form posts `Name`, `IdOwner`, `Species`, `Breed`, `Age`. The model also has `Owner` (the navigation property). MVC sees `public Owner Owner` (non-nullable), expects it in the form, doesn't find it, and adds an error: "The Owner field is required."

Two fixes:

1. Mark `Owner` as `Owner?` (nullable) — what this project does
2. Use a separate ViewModel with only form fields — what production projects do

### Common mistakes

**Skipping `ModelState.IsValid`.** Service receives invalid data and either crashes or persists garbage.

**Trusting client-side validation.** JavaScript validation is cosmetic. Anyone can disable JS or send a raw POST. Server-side validation is mandatory.

**Validating in the service when it should be in the model.** "Phone must be 10 digits" is a model rule. Putting it in the service means it's not enforced if the entity is created elsewhere (seed data, API).

**Not adding model errors back to ModelState after a service-layer failure.** If the service throws "Phone already in use," the controller should call `ModelState.AddModelError("Phone", ex.Message)` so the user sees it next to the Phone field, not as a generic banner.

### Improvements for production

**Add explicit data annotations to all models.**

```csharp
public class Owner
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [Phone]
    public string Phone { get; set; } = string.Empty;
    
    public List<Pet> Pets { get; set; } = [];
}
```

**Create custom validation attributes.** A `[ValidVetSchedule]` attribute can check that a date falls within the vet's working hours.

**FluentValidation library.** Instead of attributes, define validators as classes:

```csharp
public class OwnerValidator : AbstractValidator<Owner>
{
    public OwnerValidator()
    {
        RuleFor(o => o.Name).NotEmpty().Length(2, 100);
        RuleFor(o => o.Phone).NotEmpty().Matches(@"^\d{10}$");
    }
}
```

Pros: rules can call services, conditional validation, complex rules. Cons: extra library, two ways to validate.

### Exercises

**1. Predict.** Given:

```csharp
public class Owner
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
```

A user submits a form with `Name = ""`. Is `ModelState.IsValid` true or false? Why?

<details>
<summary>Solution</summary>

False. `Name` is non-nullable (because nullable reference types are enabled). MVC adds implicit `[Required]`. Empty string is treated as missing. `ModelState["Name"].Errors` will contain "The Name field is required."
</details>

**2. Diagnose.** Pet creation always fails with no visible error. The form just re-renders empty. The user is confused.

<details>
<summary>Solution</summary>

The view probably doesn't have `<div asp-validation-summary="All">`. Errors exist in ModelState but aren't displayed. Add the summary tag near the top of the form, or add `<span asp-validation-for>` next to each field.

If the navigation property is non-nullable, that's another silent failure — `ModelState.IsValid` is false because of the missing Owner object even though the user filled all visible fields. Check the model for non-nullable navigation properties.
</details>

**3. Implement.** Add validation that a Pet's age must be between 0 and 30.

<details>
<summary>Solution</summary>

Add the `[Range]` annotation to the model:

```csharp
[Range(0, 30, ErrorMessage = "Age must be between 0 and 30.")]
public int Age { get; set; }
```

The view's `<span asp-validation-for="Age">` will automatically display the error if validation fails.
</details>

---

## 10. Business Rules in ConsultationService

### What it is

Business rules are domain-specific constraints that go beyond simple data validation. They encode the way a real clinic operates: appointments shouldn't overlap, a pet that no-shows three times shouldn't be able to book again immediately, treatments only happen for completed consultations.

In this project, the heaviest business logic lives in `ConsultationService.Create`.

### How it's used in this project

The full method:

```csharp
public Consultation Create(Consultation entity)
{
    if (entity.DateStart < DateTime.Now)
        throw new InvalidOperationException("Cannot schedule an appointment in the past.");

    if (entity.DateEnd <= entity.DateStart)
        throw new InvalidOperationException("End time must be after start time.");

    var activeCount = _context.consultations.Count(c =>
        c.IdPet == entity.IdPet && c.Status == Status.Scheduled);
    if (activeCount >= 2)
        throw new InvalidOperationException("This pet already has 2 active appointments.");

    var noShowCount = _context.consultations.Count(c =>
        c.IdPet == entity.IdPet && c.Status == Status.NoShow);
    if (noShowCount >= 3)
    {
        var lastNoShow = _context.consultations
            .Where(c => c.IdPet == entity.IdPet && c.Status == Status.NoShow)
            .OrderByDescending(c => c.DateStart)
            .First();
        if (lastNoShow.DateStart >= DateTime.Now.AddDays(-7))
            throw new InvalidOperationException("This pet is blocked from new appointments for 7 days due to 3 no-shows.");
    }

    var vetOverlap = _context.consultations.Any(c =>
        c.IdVeterinary == entity.IdVeterinary &&
        c.Status == Status.Scheduled &&
        c.DateStart < entity.DateEnd &&
        c.DateEnd > entity.DateStart);
    if (vetOverlap)
        throw new InvalidOperationException("The veterinarian already has an appointment at this time.");

    var petOverlap = _context.consultations.Any(c =>
        c.IdPet == entity.IdPet &&
        c.Status == Status.Scheduled &&
        c.DateStart < entity.DateEnd &&
        c.DateEnd > entity.DateStart);
    if (petOverlap)
        throw new InvalidOperationException("This pet already has an appointment at this time.");

    try
    {
        _context.consultations.Add(entity);
        _context.SaveChanges();

        _ = _emailService.SendAsync(
            "Appointment Created - VetCare",
            $"A new appointment has been scheduled.\n\nReason: {entity.Reason}\nDate: {entity.DateStart:yyyy-MM-dd HH:mm} to {entity.DateEnd:HH:mm}\nStatus: {entity.Status}"
        );

        return entity;
    }
    catch (DbUpdateException ex)
    {
        throw new InvalidOperationException("Could not save consultation.", ex);
    }
}
```

`Services/ConsultationService.cs`

Six rules in order:

1. **Past date check** — straightforward comparison
2. **End-after-start** — also straightforward
3. **Max 2 active per pet** — count of Scheduled consultations
4. **No-show block** — count of NoShow + recent timestamp check
5. **Vet overlap** — interval intersection on vet
6. **Pet overlap** — same on pet

### Why this approach

#### Why interval overlap is `<` not `<=`

Two intervals `[a, b]` and `[c, d]` overlap if and only if `a < d && c < b`.

Why strict less-than? Consider boundary case: appointment 1 is 10:00–11:00, appointment 2 is 11:00–12:00. With `<`, they don't overlap (because `11:00 < 11:00` is false). With `<=`, they would overlap (`11:00 <= 11:00` is true).

In real life, two appointments back-to-back at the same minute *are* fine — the vet finishes one and starts the next. So strict less-than is the correct rule.

This is the kind of detail interviewers love to probe. "What's your overlap formula?" If you write `>=`, you're rejecting legal back-to-back appointments.

#### Why business rules belong in the service, not the controller

If you put the past-date check in the controller:

```csharp
public IActionResult Create(Consultation c)
{
    if (c.DateStart < DateTime.Now)
        return View(c);  // re-render with error
    
    _service.Create(c);
    ...
}
```

You've leaked the business rule into the HTTP layer. Three problems:

1. **Bypassable.** A future API or batch import that doesn't go through this controller skips the check.
2. **Untestable.** Testing the rule means instantiating a controller and mocking HTTP context.
3. **Forgettable.** When you add a second controller (mobile app, admin panel), you'll forget to add the check there.

In the service, the rule fires no matter who calls Create.

#### Why DateTime.Now and not DateTime.UtcNow

Mixed in this project. `Consultation.DateStart` stores local time (the user picked it from a datetime-local input which is local). The check `entity.DateStart < DateTime.Now` is consistent — both local.

`Treatment.CreateAt` stores UTC. The intent is different — it's an audit timestamp not a calendar date.

For a single-time-zone clinic this works. For multi-region, you'd standardize on UTC everywhere and convert to local only for display.

### Common mistakes

**Forgetting to also apply rules on Update.** The current `UpdateAsync` doesn't re-check overlap or past-date. A user could edit an existing appointment to a past time. To fix, extract the validations into a private method and call it from both `Create` and `UpdateAsync`.

**Race conditions.** Two users create consultations for the same vet at exactly 10:00 simultaneously. Both pass the overlap check (each sees no overlap), both insert. Result: overlapping appointments. The fix is database-level constraints (unique index on vet+timeslot) or transactional pessimistic locking.

**Off-by-one in the no-show block.** "3 no-shows" — does that mean `>= 3` or `> 3`? The code uses `>= 3`, which means the 4th attempt triggers the block. If the requirement is "after 3 no-shows," that's correct.

**Loading all consultations to count them.** `_context.consultations.Count(c => ...)` translates to a SQL `COUNT(*)` — efficient. But `_context.consultations.ToList().Count(c => ...)` loads everything to memory first — bad. Be careful.

### Improvements for production

**Extract validations into a separate method.**

```csharp
private void ValidateConsultation(Consultation entity, int? excludeId = null)
{
    // all the rules here
    // excludeId lets us skip the current consultation when validating an Update
}

public Consultation Create(Consultation entity)
{
    ValidateConsultation(entity);
    // save
}

public async Task<Consultation> UpdateAsync(Consultation entity)
{
    ValidateConsultation(entity, excludeId: entity.Id);
    // save
}
```

**Specification pattern.** Each rule is its own class with a method `IsSatisfiedBy(Consultation)`. The service runs all specifications. Pros: testable individually, composable. Cons: more files for small projects.

**Database constraints.** Add a composite unique index on `(IdVeterinary, DateStart)` to make overlap impossible at the DB level, even with race conditions.

**Transactional locking.** Wrap the validation + insert in a transaction with `SERIALIZABLE` isolation level. Slow but correct.

### Exercises

**1. Predict.** A consultation has DateStart = 10:00, DateEnd = 11:00. A new consultation is being created for the same vet, DateStart = 11:00, DateEnd = 12:00. Does the overlap rule reject it?

<details>
<summary>Solution</summary>

No. The check is `DateStart < entity.DateEnd && DateEnd > entity.DateStart`. So `10:00 < 12:00` is true, but `11:00 > 11:00` is false. Both conditions must be true to reject. Result: allowed.
</details>

**2. Diagnose.** A vet complains that the system blocks her from updating an existing appointment to a different time, even though there's no real conflict. Why?

<details>
<summary>Solution</summary>

The vet overlap check finds the existing appointment itself (which has the same vet and overlaps with the new times because... it's the same appointment). The fix is to exclude the current consultation by Id:

```csharp
var vetOverlap = _context.consultations.Any(c =>
    c.Id != entity.Id &&  // <-- this line
    c.IdVeterinary == entity.IdVeterinary &&
    ...
```

Update doesn't currently call the validation, so this issue manifests when you eventually add it.
</details>

**3. Implement.** Write a new business rule: a vet cannot have more than 8 hours of appointments in a single day. Where does it go and what's the code?

<details>
<summary>Solution</summary>

Goes in `ConsultationService.Create` alongside the other rules:

```csharp
var dayStart = entity.DateStart.Date;
var dayEnd = dayStart.AddDays(1);
var totalHoursOnDay = _context.consultations
    .Where(c => c.IdVeterinary == entity.IdVeterinary
                && c.Status == Status.Scheduled
                && c.DateStart >= dayStart
                && c.DateStart < dayEnd)
    .ToList()
    .Sum(c => (c.DateEnd - c.DateStart).TotalHours);

var newAppointmentHours = (entity.DateEnd - entity.DateStart).TotalHours;
if (totalHoursOnDay + newAppointmentHours > 8)
    throw new InvalidOperationException("Vet cannot exceed 8 hours of appointments per day.");
```

`.ToList()` is needed because `TimeSpan.TotalHours` doesn't translate to SQL.
</details>

---

## 11. Error Handling Strategy

### What it is

Errors are inevitable. Bad input, network failures, full disks, deadlocks. A robust system has a strategy for handling them: which exceptions to catch, where, and how to respond.

This project uses **layered exception translation**: each layer catches what it understands and re-throws something the caller can act on.

### How it's used in this project

#### The exception types

Three exceptions appear repeatedly:

- **`DbUpdateException`** — raised by EF Core when SaveChanges fails. Could be a constraint violation, a concurrency conflict, or a connection drop.
- **`KeyNotFoundException`** — used here to signal "the requested entity doesn't exist." Not part of the standard "missing key in dictionary" semantics, but a reasonable repurpose.
- **`InvalidOperationException`** — used as the catch-all "domain failure." Both validation rule violations and infrastructure errors get translated to this.

#### The pattern

Service:

```csharp
public Owner Create(Owner entity)
{
    try
    {
        _context.owners.Add(entity);
        _context.SaveChanges();
        return entity;
    }
    catch (DbUpdateException ex)
    {
        throw new InvalidOperationException("Could not save owner.", ex);
    }
}
```

`Services/OwnerService.cs`

The service catches `DbUpdateException` and wraps it in `InvalidOperationException`. The original exception is preserved as `InnerException` (the `ex` argument) so it's still available for logging.

Controller:

```csharp
try
{
    _service.Create(owner);
    return RedirectToAction("Index");
}
catch (InvalidOperationException ex)
{
    ModelState.AddModelError(string.Empty, ex.Message);
    return View(owner);
}
```

`Controllers/OwnerController.cs`

The controller catches `InvalidOperationException` and turns it into a user-visible error in the form.

#### Re-throwing without losing the stack trace

```csharp
catch (KeyNotFoundException) { throw; }
```

This pattern says "I caught it but I don't have anything special to do with it — let it propagate." The `throw;` (no expression) preserves the original stack trace.

Compare with:

```csharp
catch (KeyNotFoundException ex) { throw ex; }  // BAD
```

`throw ex` rewrites the stack trace as if the exception originated here. You lose the actual line that threw it. Always use bare `throw;`.

### Why this approach

#### Why translate exceptions

Without translation, `DbUpdateException` propagates from EF Core up through the service, the controller, into the MVC framework, and out to the user. Three problems:

1. **Information leak.** The exception message includes SQL details. An attacker can probe for column names.
2. **Tight coupling.** The controller now needs to know about EF Core. Switching to a different ORM means rewriting controllers.
3. **Bad UX.** "Cannot insert duplicate key in object 'dbo.Owners'" is meaningless to a user.

Translation gives the user a clean message, hides implementation details, and decouples layers.

#### Why specific catches and re-throw bare

The pattern:

```csharp
catch (KeyNotFoundException) { throw; }
catch (Exception ex)
{
    throw new InvalidOperationException("Could not retrieve owner.", ex);
}
```

`KeyNotFoundException` is already a domain exception (we want it to bubble up). The general catch handles everything else. Order matters — most specific first. C# evaluates catches top-to-bottom and uses the first match.

If you reversed the order:

```csharp
catch (Exception ex) { ... }
catch (KeyNotFoundException) { throw; }  // unreachable!
```

The compiler may warn that the second catch is unreachable.

#### Why InvalidOperationException for everything

Pragmatic choice for a small project. Every domain failure becomes `InvalidOperationException`. The controller catches one type and is done.

For larger systems, you'd define custom exceptions:

```csharp
public class DomainException : Exception { ... }
public class ValidationException : DomainException { ... }
public class ConflictException : DomainException { ... }
public class NotFoundException : DomainException { ... }
```

Then the controller can catch `ValidationException` and return 400, `NotFoundException` and return 404, etc.

### Common mistakes

**Catching Exception with no handling.**

```csharp
catch (Exception) { }
```

Silently eats the error. The bug becomes invisible.

**Catching Exception and logging only.**

```csharp
catch (Exception ex)
{
    _logger.LogError(ex);
    // returns success
}
```

User sees success. The DB write didn't happen. Reconciliation nightmare.

**throw ex instead of throw.**

```csharp
catch (Exception ex)
{
    LogIt(ex);
    throw ex;  // loses stack trace
}
```

Use `throw;`.

**Catching exceptions you don't need to.** If the service catches DbUpdateException, the controller doesn't need to. Don't double-catch unless you have a reason.

**Wrapping every line in try-catch.** Bloats the code, hides happy paths. Wrap at meaningful boundaries (whole method, transaction).

### Improvements for production

**Add logging.** Currently exceptions are translated but never logged. In production:

```csharp
catch (DbUpdateException ex)
{
    _logger.LogError(ex, "Failed to save owner");
    throw new InvalidOperationException("Could not save owner.", ex);
}
```

**Global exception handler.** ASP.NET Core has middleware for this:

```csharp
app.UseExceptionHandler("/Home/Error");
```

Already configured. The `Error` action shows a generic page. For APIs, you'd write a custom handler that returns ProblemDetails JSON.

**Structured logging with Serilog.** Instead of `_logger.LogError(ex, "Failed")`, log structured data:

```csharp
_logger.LogError(ex, "Owner save failed for {OwnerId} at {Timestamp}", entity.Id, DateTime.UtcNow);
```

Searchable, queryable, dashboard-friendly.

**Result type for expected failures.** Reserve exceptions for truly exceptional cases. For "invalid form data" or "duplicate phone," return a `Result<Owner>` type.

### Exercises

**1. Predict.** What stack trace does the user see if `throw ex` is used vs `throw`?

```csharp
try { DoWork(); }
catch (Exception ex) { throw ex; }
```

vs

```csharp
try { DoWork(); }
catch (Exception ex) { throw; }
```

<details>
<summary>Solution</summary>

`throw ex` shows the stack trace starting from the line `throw ex;`. The original origin in `DoWork()` is lost.

`throw;` shows the full stack including the line in `DoWork()` where the exception was first raised. Always use bare `throw;`.
</details>

**2. Diagnose.** A user reports they can't update an owner — the page just shows "An error occurred." No details. Where would you look first to add useful logging?

<details>
<summary>Solution</summary>

In `OwnerService.UpdateAsync`'s catch block. Add a logger:

```csharp
catch (DbUpdateException ex)
{
    _logger.LogError(ex, "Update failed for owner {OwnerId}", entity.Id);
    throw new InvalidOperationException("Could not update owner.", ex);
}
```

Then check the logs (console, file, or wherever the logger writes). The original `ex` will tell you the actual SQL error (constraint, duplicate, etc.).
</details>

**3. Implement.** Add a custom `ConflictException` that the service throws when the new consultation overlaps with an existing one. Make the controller catch it specifically and return HTTP 409 (Conflict).

<details>
<summary>Solution</summary>

```csharp
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}

// in service
if (vetOverlap)
    throw new ConflictException("The veterinarian already has an appointment at this time.");

// in controller
catch (ConflictException ex)
{
    ModelState.AddModelError(string.Empty, ex.Message);
    return Conflict(ex.Message);  // HTTP 409
}
```

Note: in MVC, you might prefer to keep returning `View` for HTML forms and only return 409 for API endpoints. Adjust as needed.
</details>

---

## 12. SMTP Email with MailKit

### What it is

SMTP (Simple Mail Transfer Protocol) is the protocol email clients use to send emails to a mail server. The mail server then delivers the email to the recipient's mailbox.

MailKit is a .NET library that implements an SMTP client. This project uses it to send appointment notifications.

### How it's used in this project

#### Configuration

```json
"EmailSettings": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "Username": "juan.barrera.riwi@gmail.com",
    "Password": "aiaw xwiv hegt cowz",
    "ClinicEmail": "clinic@vetcare.com"
}
```

`appsettings.json`

Bound to a POCO:

```csharp
public class EmailSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ClinicEmail { get; set; } = string.Empty;
}
```

`Models/EmailSettings.cs`

Registered as Singleton in Program.cs:

```csharp
var emailSettings = builder.Configuration.GetSection("EmailSettings").Get<EmailSettings>()!;
builder.Services.AddSingleton(emailSettings);
builder.Services.AddScoped<IEmailService, EmailService>();
```

`Program.cs`

#### The interface

```csharp
public interface IEmailService
{
    Task SendAsync(string subject, string body);
}
```

`Interfaces/IEmailService.cs`

Minimal — just a subject and body. The recipient is fixed (the clinic email from settings).

#### The implementation

```csharp
public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;

    public EmailService(EmailSettings settings)
    {
        _settings = settings;
    }

    public async Task SendAsync(string subject, string body)
    {
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(_settings.Username));
        message.To.Add(MailboxAddress.Parse(_settings.ClinicEmail));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };

        using var client = new SmtpClient();
        await client.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(_settings.Username, _settings.Password);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}
```

`Services/EmailService.cs`

Steps:
1. Build a `MimeMessage` (the email itself)
2. Open an SMTP connection (TCP + STARTTLS)
3. Authenticate
4. Send
5. Disconnect

#### The trigger points

Three places call `SendAsync`:

**Consultation Created:**

```csharp
_ = _emailService.SendAsync(
    "Appointment Created - VetCare",
    $"A new appointment has been scheduled.\n\nReason: {entity.Reason}\nDate: {entity.DateStart:yyyy-MM-dd HH:mm} to {entity.DateEnd:HH:mm}\nStatus: {entity.Status}"
);
```

`Services/ConsultationService.cs`

**Consultation Cancelled (only if status changed to Canceled):**

```csharp
if (previousStatus != Status.Canceled && existing.Status == Status.Canceled)
{
    _ = _emailService.SendAsync(
        "Appointment Cancelled - VetCare",
        $"An appointment has been cancelled.\n\nReason: {existing.Reason}\nDate: {existing.DateStart:yyyy-MM-dd HH:mm}"
    );
}
```

`Services/ConsultationService.cs`

**Treatment Assigned:**

```csharp
_ = _emailService.SendAsync(
    "Treatment Assigned - VetCare",
    $"A new treatment has been assigned.\n\nDescription: {entity.Description}\nConsultation ID: {entity.IdConsultation}\nDate: {entity.CreateAt:yyyy-MM-dd HH:mm}"
);
```

`Services/TreatmentService.cs`

### Why this approach

#### Why port 587 + STARTTLS

Mail server ports:

| Port | Encryption |
|---|---|
| 25 | None (server-to-server, often blocked) |
| 465 | SSL from the start (legacy) |
| 587 | STARTTLS (modern standard) |

STARTTLS works like this:

1. Client connects to the server in plain text on port 587
2. Server announces it supports STARTTLS
3. Client says "let's upgrade"
4. Both negotiate TLS
5. Rest of the conversation is encrypted

The line `await client.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.StartTls)` does this handshake.

Why not port 465 (SSL from start)? Both work; 587 is the modern standard recommended by RFC 8314. Either is fine for Gmail.

#### Why Gmail App Passwords

Google blocks regular passwords for SMTP since 2022. Even with the correct password, authentication fails with a "less secure app" error.

App Passwords are 16-character codes generated specifically for one app. They:
- Bypass the security block
- Only work with 2FA enabled
- Can be revoked individually

To generate one: Google Account → Security → 2-Step Verification → App Passwords.

The password format: `aiaw xwiv hegt cowz` — Gmail shows it with spaces but they're just visual. SMTP accepts it as one continuous string.

#### Why fire-and-forget

The line `_ = _emailService.SendAsync(...)` discards the returned Task. This is **fire-and-forget**:

- The email runs in the background
- The current method continues immediately
- Failures aren't visible to the caller

For a notification, this is correct:
- The DB write already succeeded (the appointment is real)
- Email failure shouldn't roll back the DB
- User shouldn't wait 3 seconds for SMTP

For a transactional email (password reset, payment receipt), you'd want to await and handle failures:

```csharp
try { await _emailService.SendAsync(...); }
catch { /* log and queue for retry */ }
```

The trade-off is that fire-and-forget hides failures. In production you'd at least log them.

### Common mistakes

**Hard-coding credentials.**

```csharp
client.AuthenticateAsync("me@gmail.com", "mypassword");
```

Credentials in source code end up in git. Anyone with read access to the repo has your password. Always read from configuration.

**Storing passwords in appsettings.json.** Slightly better than source code but still bad. The file is in git. The fix is **User Secrets** (development) or **environment variables** (production):

```bash
dotnet user-secrets set "EmailSettings:Password" "aiawxwivhegtcowz"
```

**Sending email synchronously and blocking the request.** A user clicks "Create" and waits 3 seconds for SMTP. Bad UX. Fire-and-forget or queue-based approaches solve this.

**No retry logic.** SMTP can fail temporarily (network blip, server overload). Production code retries with exponential backoff.

**HTML emails without proper escaping.** If you build an HTML email by string concatenation with user data, you have an XSS-equivalent bug in email clients. Use a templating library.

### Improvements for production

**Move secrets out of appsettings.json.**

For development:

```bash
dotnet user-secrets init
dotnet user-secrets set "EmailSettings:Password" "...."
```

For production: environment variables, Azure Key Vault, AWS Secrets Manager.

**Async queue with retry.** Instead of fire-and-forget, push email jobs to a queue (RabbitMQ, Azure Storage Queue). A background worker processes them with retry logic. Failures don't disappear.

**Email templates.** Currently emails are built by string concatenation. A templating library like RazorLight lets you write `.cshtml` for emails:

```csharp
var body = await _engine.CompileRenderAsync("AppointmentCreated", new { Pet = ..., Vet = ... });
```

**Logging.** Wrap the SMTP send in try-catch and log failures. Currently they vanish.

```csharp
public async Task SendAsync(string subject, string body)
{
    try
    {
        // ... actual send
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Email send failed: {Subject}", subject);
    }
}
```

**Multiple recipients.** Currently all emails go to `ClinicEmail`. A real system sends to the owner's email (which would need adding to the Owner model).

### Exercises

**1. Predict.** What happens if the SMTP server is down when `_ = _emailService.SendAsync(...)` is called?

<details>
<summary>Solution</summary>

The `SendAsync` task fails. Because it's fire-and-forget (`_ = `), the failure is silent — no logging, no retry, no user notification. The consultation is still saved successfully. From the user's perspective, everything looks fine; from operations' perspective, the notification was lost.
</details>

**2. Diagnose.** Emails work locally but fail in production with `AuthenticationException`. The same code, same Gmail account.

<details>
<summary>Solution</summary>

Most likely the production environment doesn't have the App Password configured (only the regular password). Check the production configuration source — environment variable, Key Vault — and verify the App Password is set there. Gmail rejects the regular password.

Other possibilities: 2FA was disabled on the Gmail account, or the App Password was revoked.
</details>

**3. Implement.** Modify `EmailService` to log failures using `ILogger<EmailService>` instead of letting them vanish.

<details>
<summary>Solution</summary>

```csharp
public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(EmailSettings settings, ILogger<EmailService> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public async Task SendAsync(string subject, string body)
    {
        try
        {
            var message = new MimeMessage();
            // ... existing code
            await client.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email: {Subject}", subject);
        }
    }
}
```

ASP.NET Core registers `ILogger<T>` automatically; no extra setup needed.
</details>

---

## 13. Reports and LINQ Aggregation

### What it is

Reports are read-only queries that aggregate data: counts, sums, top-N. The project's `ReportController` produces 4 reports: top vets, top pets, top medicines, no-show rate.

LINQ (Language Integrated Query) is C#'s syntax for these operations. `GroupBy`, `Select`, `OrderBy`, `Take`, `Count`, `Sum` are the primitives.

### How it's used in this project

#### The full controller

```csharp
public class ReportController : Controller
{
    private readonly MysqlDbcontext _context;

    public ReportController(MysqlDbcontext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var consultations = await _context.consultations
            .Include(c => c.Pet)
            .Include(c => c.Veterinary)
            .ToListAsync();

        ViewBag.TopVets = consultations
            .GroupBy(c => c.Veterinary?.Name ?? "Unknown")
            .Select(g => new { Name = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToList();

        ViewBag.TopPets = consultations
            .GroupBy(c => c.Pet?.Name ?? "Unknown")
            .Select(g => new { Name = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToList();

        var medicines = await _context.treatmentsMedicines
            .Include(tm => tm.Medicine)
            .ToListAsync();

        ViewBag.TopMedicines = medicines
            .GroupBy(tm => tm.Medicine?.Name ?? "Unknown")
            .Select(g => new { Name = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToList();

        var total = consultations.Count;
        var noShows = consultations.Count(c => c.Status == Status.NoShow);
        ViewBag.Total = total;
        ViewBag.NoShows = noShows;
        ViewBag.NoShowRate = total > 0 ? Math.Round((double)noShows / total * 100, 1) : 0;

        return View();
    }
}
```

`Controllers/ReportController.cs`

#### Breaking down a report

Top vets by appointment count:

```csharp
ViewBag.TopVets = consultations
    .GroupBy(c => c.Veterinary?.Name ?? "Unknown")
    .Select(g => new { Name = g.Key, Count = g.Count() })
    .OrderByDescending(x => x.Count)
    .Take(5)
    .ToList();
```

Step by step:

1. `consultations` is `List<Consultation>` (already loaded from DB)
2. `GroupBy(c => c.Veterinary?.Name ?? "Unknown")` — groups consultations by vet name; null vets become "Unknown"
3. `Select(g => new { Name = g.Key, Count = g.Count() })` — projects each group into an anonymous type with the vet name and the count of consultations in that group
4. `OrderByDescending(x => x.Count)` — sorts groups by count, highest first
5. `Take(5)` — keeps the top 5
6. `.ToList()` — materializes to a list

The result is a list of `{ Name, Count }` objects.

#### LINQ to Entities vs LINQ to Objects

Notice the controller calls `.ToListAsync()` *first*, then runs the LINQ aggregation:

```csharp
var consultations = await _context.consultations.ToListAsync();
ViewBag.TopVets = consultations.GroupBy(...)...;
```

This is **LINQ to Objects** — the GroupBy runs in C# memory, not SQL.

The alternative is **LINQ to Entities** — keep the IQueryable and let EF Core translate to SQL:

```csharp
ViewBag.TopVets = await _context.consultations
    .GroupBy(c => c.Veterinary.Name)
    .Select(g => new { Name = g.Key, Count = g.Count() })
    .ToListAsync();
```

This produces a single SQL query with `GROUP BY`. Much more efficient for large datasets.

Why does the project load to memory first? Because EF Core's translation has limits. The `?? "Unknown"` for null navigation properties can throw "could not translate" errors. Loading first is simpler and safe for small datasets.

### Why this approach

#### Why anonymous types

`new { Name = g.Key, Count = g.Count() }` creates a class on the fly with two properties. No need to define a `TopVetReport` class.

Pros:
- Less code
- Read-only (immutable)

Cons:
- Limited to the method (anonymous types can't be returned across method boundaries cleanly)
- No XML doc comments
- Bind via reflection only

For internal queries that feed a view, anonymous types are fine. For shared models, define a class.

#### Why ViewBag for multiple results

The Index action produces 4 separate lists. A single `@model` can only be one type. Three options:

1. **ViewBag** — used here. `ViewBag.TopVets`, `ViewBag.TopPets`, etc.
2. **ViewModel** — a `ReportsViewModel { TopVets, TopPets, TopMedicines, ... }` class
3. **Multiple actions** — one per report

ViewBag works for simple cases. ViewModel is more refactor-friendly. Multiple actions are appropriate when each report needs its own URL.

### Common mistakes

**Loading the whole table to count.**

```csharp
var count = (await _context.consultations.ToListAsync()).Count;
```

Loads every row to memory just to count. The right way:

```csharp
var count = await _context.consultations.CountAsync();
```

One efficient SQL query.

**Using == on navigation properties.**

```csharp
.GroupBy(c => c.Veterinary == null ? "Unknown" : c.Veterinary.Name)
```

EF Core might not translate this correctly. Use `.Include` to load the navigation, then do the null check in memory.

**N+1 in LINQ.**

```csharp
var vets = await _context.veterinaries.ToListAsync();
var report = vets.Select(v => new {
    Name = v.Name,
    Count = _context.consultations.Count(c => c.IdVeterinary == v.Id)
}).ToList();
```

This issues one query per vet. Bad. Use `Include` + `GroupBy` or a single aggregate query.

**Forgetting to handle empty results.**

```csharp
ViewBag.NoShowRate = (double)noShows / total * 100;
```

If `total = 0`, this is `0/0 = NaN`. The current code handles it: `total > 0 ? ... : 0`.

### Improvements for production

**Move LINQ to the database side.**

```csharp
var topVets = await _context.consultations
    .Where(c => c.IdVeterinary != null)
    .GroupBy(c => c.Veterinary!.Name)
    .Select(g => new { Name = g.Key, Count = g.Count() })
    .OrderByDescending(x => x.Count)
    .Take(5)
    .ToListAsync();
```

Single SQL `GROUP BY` query. Scales to millions of rows.

**Pagination for large reports.**

```csharp
var page = await _context.consultations
    .Skip((pageNumber - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();
```

**Caching.** Reports often don't need to be real-time. Cache for 5 minutes:

```csharp
var topVets = await _cache.GetOrCreateAsync("topvets", async entry =>
{
    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
    return await ComputeTopVetsAsync();
});
```

**Background job for expensive reports.** A daily report can run overnight. Store the result, serve from cache.

**Materialized views.** For reports that hammer the same aggregations, create a `vet_appointment_counts` view in the database and query it directly.

### Exercises

**1. Predict.** What's wrong with this query for performance?

```csharp
var consultations = await _context.consultations.ToListAsync();
var todayCount = consultations.Count(c => c.DateStart.Date == DateTime.Today);
```

<details>
<summary>Solution</summary>

It loads every consultation to memory just to count today's. The fix is to do the filter in SQL:

```csharp
var today = DateTime.Today;
var tomorrow = today.AddDays(1);
var todayCount = await _context.consultations
    .CountAsync(c => c.DateStart >= today && c.DateStart < tomorrow);
```

(EF Core can't translate `.Date`, so we compute the bounds explicitly.)
</details>

**2. Diagnose.** A junior added a "Top Owners" report and notices the page takes 30 seconds to load. They have 10,000 owners with millions of pets and consultations.

```csharp
var owners = await _context.owners.Include(o => o.Pets).ToListAsync();
ViewBag.TopOwners = owners
    .Select(o => new { Name = o.Name, ConsultationCount = ... })
    .OrderByDescending(x => x.ConsultationCount)
    .Take(5)
    .ToList();
```

How would you fix it?

<details>
<summary>Solution</summary>

The query loads all 10,000 owners and all their pets to memory. Move the aggregation to SQL:

```csharp
ViewBag.TopOwners = await _context.consultations
    .Include(c => c.Pet!.Owner)
    .GroupBy(c => c.Pet!.Owner!.Name)
    .Select(g => new { Name = g.Key, Count = g.Count() })
    .OrderByDescending(x => x.Count)
    .Take(5)
    .ToListAsync();
```

Single SQL query, executed on the DB. From 30 seconds to milliseconds.
</details>

**3. Implement.** Add a 5th report: average consultation duration, in minutes, grouped by vet.

<details>
<summary>Solution</summary>

```csharp
ViewBag.AvgDurationByVet = consultations
    .GroupBy(c => c.Veterinary?.Name ?? "Unknown")
    .Select(g => new {
        Name = g.Key,
        AvgMinutes = g.Average(c => (c.DateEnd - c.DateStart).TotalMinutes)
    })
    .OrderByDescending(x => x.AvgMinutes)
    .Take(5)
    .ToList();
```

Then add a section in `Views/Report/Index.cshtml` to display it.
</details>

---

## 14. Medical History

### What it is

Medical history is a per-pet view showing all past consultations and treatments. Implemented as `PetController.History(int id)` and `Views/Pet/History.cshtml`.

### How it's used in this project

#### The action

```csharp
[HttpGet]
public async Task<IActionResult> History(int id)
{
    try
    {
        var pet = await _service.GetByIdAsync(id);

        var consultations = await _context.consultations
            .Where(c => c.IdPet == id)
            .Include(c => c.Veterinary)
            .OrderByDescending(c => c.DateStart)
            .ToListAsync();

        var consultationIds = consultations.Select(c => c.Id).ToList();

        var treatments = await _context.treatments
            .Where(t => consultationIds.Contains(t.IdConsultation))
            .ToListAsync();

        ViewBag.Pet = pet;
        ViewBag.Consultations = consultations;
        ViewBag.Treatments = treatments;

        return View();
    }
    catch (KeyNotFoundException)
    {
        return NotFound();
    }
}
```

`Controllers/PetController.cs`

Three queries:

1. The pet (via service)
2. All consultations for the pet, with vet info, newest first
3. All treatments whose consultation belongs to this pet

#### The view

```html
@{
    var pet = (Pet)ViewBag.Pet;
    var consultations = (IEnumerable<Consultation>)ViewBag.Consultations;
    var treatments = (IEnumerable<Treatment>)ViewBag.Treatments;
}

<div class="row">
    <div class="col-12 mb-3">
        <h5>@pet.Name — @pet.Species — @pet.Breed</h5>
    </div>

    <div class="col-12">
        <table class="table table-striped">
            <thead>
                <th>DATE</th>
                <th>REASON</th>
                <th>VETERINARY</th>
                <th>STATUS</th>
                <th>TREATMENT</th>
            </thead>
            <tbody>
            @foreach (var c in consultations)
            {
                var treatment = treatments.FirstOrDefault(t => t.IdConsultation == c.Id);
                <tr>
                    <td>@c.DateStart.ToString("yyyy-MM-dd HH:mm")</td>
                    <td>@c.Reason</td>
                    <td>@c.Veterinary?.Name</td>
                    <td>@c.Status</td>
                    <td>@(treatment?.Description ?? "-")</td>
                </tr>
            }
            </tbody>
        </table>
    </div>
</div>
```

`Views/Pet/History.cshtml`

For each consultation, the view does a `FirstOrDefault` against the treatments list to find a matching one.

### Why this approach

#### Why two queries instead of one big JOIN

A single LINQ query with multiple includes:

```csharp
var consultations = await _context.consultations
    .Where(c => c.IdPet == id)
    .Include(c => c.Veterinary)
    .Include(c => c.Treatments)  // would need a navigation property
    .ToListAsync();
```

Requires `Consultation.Treatments` to exist as a collection navigation. The current `OnModelCreating` uses `.WithMany()` (without an argument), meaning the inverse navigation isn't exposed.

Workarounds:
1. Add the inverse navigation and update the Fluent API
2. Run two queries (current approach)

The two-query approach is simpler given the current model. The trade-off:
- Pro: doesn't require model changes
- Con: two round trips to the DB

For a small clinic, two queries are negligible. For high-scale, you'd consolidate.

#### Why ViewBag instead of ViewModel

Same trade-off as ReportController. ViewBag wins on simplicity, ViewModel wins on type safety.

### Common mistakes

**Loading too many treatments.** The query loads *all* treatments for any consultation belonging to this pet. Fine for small datasets. For a pet with 1000+ visits over 20 years, you'd want pagination.

**N+1 in the view.** If you wrote:

```csharp
foreach (var c in consultations)
{
    var treatments = await _context.treatments.Where(t => t.IdConsultation == c.Id).ToListAsync();
    // ...
}
```

That's one query per consultation. The current approach (one query for all, then in-memory `FirstOrDefault`) is correct.

**Forgetting to order.** Without `OrderByDescending(c => c.DateStart)`, history appears in insertion order — confusing for users who expect chronological order.

### Improvements for production

**ViewModel.** Replace ViewBag with:

```csharp
public class PetHistoryViewModel
{
    public Pet Pet { get; set; }
    public List<HistoryEntry> Entries { get; set; } = [];
}

public class HistoryEntry
{
    public Consultation Consultation { get; set; }
    public Treatment? Treatment { get; set; }
}
```

The controller maps the data once, the view just iterates.

**Pagination.** For pets with long histories, paginate the table.

**Filter by date range.** Allow the user to see "consultations in 2024" or "last 6 months."

**Export to PDF.** Real clinics often need a printable history sheet. A library like QuestPDF can generate one.

### Exercises

**1. Predict.** What does the History view render if the pet has no consultations?

<details>
<summary>Solution</summary>

The pet header renders, then the table with headers, but the `@foreach` doesn't iterate. The user sees an empty table. UX-wise, you'd want to show "No history available" instead.
</details>

**2. Diagnose.** A user notices that consultations appear in random order, not newest-first. The code looks correct.

<details>
<summary>Solution</summary>

Verify `OrderByDescending(c => c.DateStart)` is actually in the query. If it's there, check that the consultations being passed to the view aren't being re-sorted by something else (e.g., a sort in the view itself, or a later operation). Also check that `DateStart` is a real `DateTime`, not a string — string-typed dates sort alphabetically.
</details>

**3. Implement.** Modify the History action to also include the count of medicines used in each treatment.

<details>
<summary>Solution</summary>

```csharp
var treatmentIds = treatments.Select(t => t.Id).ToList();
var medicineCountsByTreatment = await _context.treatmentsMedicines
    .Where(tm => treatmentIds.Contains(tm.IdTreatment))
    .GroupBy(tm => tm.IdTreatment)
    .Select(g => new { TreatmentId = g.Key, Count = g.Count() })
    .ToDictionaryAsync(x => x.TreatmentId, x => x.Count);

ViewBag.MedicineCounts = medicineCountsByTreatment;
```

In the view, use `ViewBag.MedicineCounts.GetValueOrDefault(treatment.Id, 0)` to display.
</details>

---

## 15. The Bug Catalog

This section documents every bug encountered during development of this project, with diagnosis and lesson. These are real bugs, not contrived examples.

### Bug 1 — EF Core Convention Conflict (IdOwner vs OwnerId)

**Symptom.** Pet creation appeared to succeed but the pet wasn't linked to the owner. Or a foreign key constraint violation if `OwnerId` was NOT NULL.

**Investigation.** Looking at the migration, two columns existed in the `pets` table:
- `IdOwner` — your model property
- `OwnerId` — created by EF Core as a shadow foreign key

The actual FK constraint was on `OwnerId`. Inserting set your `IdOwner` correctly but left `OwnerId` null/zero.

**Root cause.** EF Core's convention for FK property naming is `<NavigationName>Id`. For `public Owner Owner`, EF expects `OwnerId`. Your `IdOwner` doesn't match. EF couldn't infer the relationship, so it created a shadow `OwnerId` to be the real FK.

**Fix.** Tell EF Core which property is the FK using Fluent API:

```csharp
modelBuilder.Entity<Pet>()
    .HasOne(p => p.Owner)
    .WithMany(o => o.Pets)
    .HasForeignKey(p => p.IdOwner);
```

`Data/MysqlDbcontext.cs:OnModelCreating`

After adding this, a migration drops the `OwnerId` shadow column and rebuilds the FK on `IdOwner`.

**Lesson.** EF Core conventions are powerful but invisible. When you fight the convention, configure explicitly. Read your migration files before applying them — duplicate columns are a clear sign something is misconfigured.

### Bug 2 — IEnumerable<Pet> with [] Initializer

**Symptom.** `NotSupportedException: Collection was of a fixed size.` thrown by EF Core when querying pets.

**Stack trace.** `System.SZArrayHelper.Add<T>(T)`.

**Investigation.** The Owner model:

```csharp
public IEnumerable<Pet> Pets { get; set; } = [];
```

When `_context.pets.Include(p => p.Owner).ToListAsync()` ran, EF Core tried to populate the `Owner.Pets` navigation by calling `.Add()` on it. But `[]` initialized as an `IEnumerable<Pet>` resolves to a fixed-size array, which doesn't support `.Add()`.

**Fix.** Change the type to `List<Pet>`:

```csharp
public List<Pet> Pets { get; set; } = [];
```

**Lesson.** Collection navigation properties must be mutable. Use `List<T>` or `ICollection<T>`, never `IEnumerable<T>`. The `[]` initializer's actual type depends on the declared type — for `IEnumerable`, it's a fixed-size empty array; for `List`, it's an empty growable list.

### Bug 3 — Navigation Properties Causing ModelState Failures

**Symptom.** Submitting the Pet form silently re-rendered the form. No visible errors, no DB write, no exception in logs.

**Investigation.** Adding logging revealed `ModelState.IsValid` was false. The error was on the `Owner` field: "The Owner field is required." But the form didn't have an Owner field — it had `IdOwner`.

**Root cause.** With nullable reference types enabled and `public Owner Owner` (non-nullable), MVC's validation pipeline added an implicit `[Required]` to the `Owner` property. The form didn't post an `Owner` object (only `IdOwner`), so MVC marked it invalid.

**Fix.** Make navigation properties nullable:

```csharp
public Owner? Owner { get; set; }
```

Same fix applied to `Pet.Owner`, `Consultation.Pet`, `Consultation.Veterinary`, `Treatment.Consultation`, `TreatmentMedicine.Medicine`, `TreatmentMedicine.Treatment`.

**Lesson.** Navigation properties are filled by EF Core, not by form posts. Mark them nullable to tell MVC "don't validate this from form data." It's also accurate — they ARE null when not loaded.

### Bug 4 — Pet Form Binding Owner as Text

**Symptom.** The Pet Create form had an "Owner" text input. Submitting failed in confusing ways.

**Investigation.** The form:

```html
<input asp-for="Owner" type="text" class="form-control">
```

`asp-for="Owner"` binds to the `Owner` complex type. MVC tried to construct an `Owner` object from a text string. Doesn't work.

**Fix.** Replace with a dropdown bound to `IdOwner`:

```html
<select asp-for="IdOwner" class="form-select">
    <option value="">Select an owner</option>
    @foreach (var owner in (IEnumerable<Owner>)ViewBag.Owners)
    {
        <option value="@owner.Id">@owner.Name</option>
    }
</select>
```

`Views/Pet/Create.cshtml`

**Lesson.** Form fields bind to scalar values (string, int, DateTime). Complex types (Owner) bind only if the form submits the nested fields (`Owner.Name`, `Owner.Phone`). For FK relationships, always bind to the FK property (`IdOwner`), not the navigation (`Owner`).

### Bug 5 — Locked Executable on Build

**Symptom.** `dotnet build` failed with:

```
error MSB3027: Could not copy ... apphost.exe ... The file is locked by: "simulationTest (32716)"
```

**Investigation.** The previously-running app held the executable file. Windows can't overwrite a running .exe.

**Fix.** Stop the running app (Ctrl+C in the terminal, or Stop in IDE), then rebuild.

**Lesson.** This isn't a code bug — it's a development workflow gotcha. When build fails with "file is locked," the actual error is "your old app is still running." On Linux, the old binary is unlinked but stays running until the process exits, so the rebuild succeeds while the old binary lives on.

### Bug 6 — Razor Parse Error in Option Attribute

**Symptom.** Build error: `RZ1031: The tag helper 'option' must not have C# in the element's attribute declaration area.`

**Investigation.** Code that triggered it:

```html
<option value="@pet.Id" @(Model.IdPet == pet.Id ? "selected" : "")>@pet.Name</option>
```

Razor's tag helper engine parses the `<option>` element and disallows C# expressions in attribute values when the parent `<select>` has a tag helper.

**Fix.** Use `@if`/`@else` to choose between two complete option elements:

```html
@foreach (var pet in (IEnumerable<Pet>)ViewBag.Pets)
{
    if (Model.IdPet == pet.Id)
    {
        <option value="@pet.Id" selected>@pet.Name</option>
    }
    else
    {
        <option value="@pet.Id">@pet.Name</option>
    }
}
```

`Views/Consultation/Update.cshtml`

**Lesson.** Razor + tag helpers have parsing rules that differ from plain Razor. When a tag helper is involved, attribute interpolation can fail. The if/else workaround is verbose but always works.

### Bug 7 — Past Date Allowed in Tests

**Symptom.** Creating a consultation in the past appeared to work in dev, then failed in QA.

**Investigation.** Local time and UTC discrepancy. The form input was treated as local time, but the validation used `DateTime.UtcNow`.

**Fix.** Use `DateTime.Now` consistently throughout the consultation logic, since the form stores local time.

```csharp
if (entity.DateStart < DateTime.Now)
    throw new InvalidOperationException("Cannot schedule an appointment in the past.");
```

**Lesson.** Pick a time zone strategy and stick with it. For a single-clinic app, local time everywhere is fine. For multi-region, use UTC everywhere and convert for display only.

### Bug 8 — Duplicate Migration ConventionFix

**Symptom.** Confusion about whether the medicines table was named `Medicines` or `medicines`.

**Investigation.** The original migration created `Medicines` (capitalized). MySQL on Linux is case-sensitive, so `medicines` (lowercase, used elsewhere) was a different table. A `ConventionFix` migration renamed it.

**Fix.** Already in place — the `ConventionFix` migration renames `Medicines` → `medicines`.

**Lesson.** MySQL case-sensitivity differs by OS. On Windows it's case-insensitive (you can refer to either name and it works). On Linux it's case-sensitive (`medicines` and `Medicines` are different tables). Pick a convention (lowercase) and enforce it.

---

## 16. Production-Readiness Gaps and Roadmap

### What's missing from this project for production

The project works as a learning artifact. For a real clinic deployment, several pieces would need to be added.

#### Authentication and authorization

Currently anyone with the URL can create, edit, or delete anything. A real system needs:

- User accounts with passwords
- Role-based access (admin, vet, receptionist)
- `[Authorize(Roles = "admin")]` on destructive actions
- Login/logout flow
- Password hashing (BCrypt or Argon2)

Recommended: ASP.NET Core Identity. It provides all of this out of the box.

#### Logging

Currently there's no logging. Exceptions are translated but not recorded. In production, you need to know what went wrong.

Recommended: Serilog. Structured logging that writes to console, file, or external services like Seq or Elasticsearch.

```csharp
builder.Host.UseSerilog((ctx, config) => config
    .ReadFrom.Configuration(ctx.Configuration)
    .WriteTo.Console()
    .WriteTo.File("logs/app-.log", rollingInterval: RollingInterval.Day));
```

#### Data validation

Models have no `[Required]`, `[StringLength]`, etc. Anyone can submit a 1MB string for `Owner.Name`. Add data annotations to all models.

#### DTOs (Data Transfer Objects)

The project passes EF Core entities directly to views. This couples the database schema to the UI. Renaming `Phone` to `PhoneNumber` requires updating the DB, the model, the controller, and every view.

Introduce DTOs:

```csharp
public class OwnerCreateDto
{
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [Required, Phone]
    public string PhoneNumber { get; set; } = string.Empty;
}
```

Views bind to the DTO. Controllers map DTO → Entity. Service operates on the entity. Decouples layers.

#### Tests

Zero unit tests. Zero integration tests.

Recommended:

- **xUnit** for unit tests
- **Moq** or **NSubstitute** for mocking dependencies
- **EF Core InMemory** or **Testcontainers** for integration tests
- **Playwright** for end-to-end UI tests

What to test first:
1. `ConsultationService.Create` — every business rule
2. `OwnerService` CRUD — happy path + error cases
3. The Update action of each controller — happy path + invalid id

#### Docker

The app needs to be containerized. A typical setup:

`Dockerfile`:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY *.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "simulationTest.dll"]
```

`docker-compose.yml`:

```yaml
services:
  app:
    build: .
    ports: ["5000:80"]
    depends_on: [db]
    environment:
      - ConnectionStrings__MysqlConnection=Server=db;Database=simulation2;User=root;Password=password
  db:
    image: mysql:8
    environment:
      - MYSQL_ROOT_PASSWORD=password
      - MYSQL_DATABASE=simulation2
    ports: ["3306:3306"]
    volumes: [db_data:/var/lib/mysql]

volumes:
  db_data:
```

`docker-compose up` and the whole system runs.

#### CI/CD

A GitHub Actions workflow that runs on every push:

```yaml
name: CI
on: [push, pull_request]
jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: '10.0' }
      - run: dotnet restore
      - run: dotnet build --no-restore
      - run: dotnet test --no-build
```

#### Security

- Plain text password in `appsettings.json` — move to User Secrets (dev) and environment variables (prod)
- No HTTPS certificate pinning
- No rate limiting on login attempts
- No CSRF tokens on forms (`[ValidateAntiForgeryToken]`)
- No content security policy headers

### The 90-day junior dev roadmap

Concrete plan to prepare for a junior full-stack interview. Allocate 2-3 hours per day.

#### Weeks 1-3: Algorithm fundamentals

- 60 LeetCode Easy problems (3 per day)
- Focus on: arrays, strings, hashmaps, two pointers, sliding window
- Pattern: solve, then read top solutions, then re-solve from memory next day
- Track time-to-solve for each problem; aim for 15 minutes

#### Weeks 4-6: Testing and CI

- Add xUnit tests to this project — all 6 services, every CRUD method, every business rule
- Add a GitHub Actions workflow that builds and tests on push
- Read "The Art of Unit Testing" by Roy Osherove (the C# 3rd edition)

#### Weeks 7-8: Frontend

- Build an Angular frontend that consumes a REST API version of this project
- Required concepts: components, services, RxJS observables, HttpClient, routing, reactive forms
- Deploy somewhere free (Vercel, Netlify) so you have a live URL

#### Weeks 9-10: Containerization

- Dockerize this project (Dockerfile + docker-compose)
- Push to Docker Hub
- Deploy a containerized version to a cloud provider (Railway, Fly.io, or DigitalOcean)

#### Weeks 11-12: Mock interviews

- 5 mock interviews on platforms like Pramp or Interviewing.io
- Practice explaining this project from scratch in 5 minutes
- Practice the system design discussion: "How would you scale this to 1000 clinics?"

### Resume points specific to this project

When listing this project on a resume:

- Built a veterinary clinic management system (ASP.NET Core MVC, EF Core, MySQL)
- Implemented business rule validation including appointment overlap detection and scheduling constraints
- Integrated SMTP-based email notifications via MailKit
- Containerized with Docker Compose for one-command setup (after you do the Docker work)
- Authored xUnit test suite with X% line coverage (after tests)

Don't list:
- "Familiar with Entity Framework" (sounds weak, prefer "Built data access layer using EF Core")
- "Used HTML and CSS" (assumed)
- "Worked on a team" (you didn't)

### What separates a strong junior from average

The mid-level interviewers are looking for these signals:

1. **You can read your own exception messages.** When something breaks, you trace it instead of asking.
2. **You think about edge cases unprompted.** "What happens if the database is down?" is your default question.
3. **You ask about constraints before solving.** Performance budget, dataset size, concurrency expectations.
4. **You can articulate trade-offs.** Every decision has a cost; can you name it?
5. **You write tests without being told.** Especially for code you didn't write.

### The interview gauntlet

Typical junior full-stack interview:

| Stage | Focus | Prep |
|---|---|---|
| Phone screen (30 min) | Behavioral + basic tech | STAR stories, "tell me about your project" |
| Coding round (60 min) | LeetCode Easy/Medium | Practice 50+ problems |
| Take-home (varies) | CRUD app or feature add | Build and ship something |
| System design (45 min) | "Design a URL shortener" | Read "Designing Data-Intensive Applications" intro chapters |
| Onsite final (3-4 hours) | Pair coding + culture | Mock interviews |

### Final advice

Three things separate the candidates who get offers from those who don't:

1. **Ship something.** A finished, deployed, polished project beats five half-built ones.
2. **Read code.** Your own, your teammates', open-source repos. Reading code builds intuition faster than writing it.
3. **Show your work.** Bad commits with no messages and unmerged branches signal carelessness. Conventional commits, clean PRs, and a structured repo signal professionalism.

The veterinary system you built is a real project. Finish it (tests, Docker, deployed URL), explain it well, and it will pass the "can you build something" bar at every junior position.

### Exercises

**1. Predict.** A recruiter asks: "Why didn't you write tests for this project?" What's the strongest answer that's still honest?

<details>
<summary>Solution</summary>

"I focused on getting the features working and understanding the patterns first. Tests are next on my list — I'm planning to add xUnit unit tests for the service layer this month, starting with `ConsultationService` because that's where the business rules live."

Honest about the gap, demonstrates self-awareness, shows a concrete plan. Don't say "I didn't have time" or "the project doesn't need tests."
</details>

**2. Diagnose.** An interviewer says your project is "too simple." How do you respond?

<details>
<summary>Solution</summary>

"It started simple, but I added business rule enforcement (appointment overlap, no-show blocks), email notifications, and reports. The next phase is authentication and Docker. What complexity were you looking for that I haven't shown? I'd rather hear directly so I can address it."

Don't get defensive. The interviewer is testing whether you can take feedback. Asking back shows confidence and curiosity.
</details>

**3. Implement.** Write the elevator pitch for this project — 60 seconds, spoken aloud. Time yourself.

<details>
<summary>Solution</summary>

Sample:

"It's a clinic management system for veterinarians. Built with ASP.NET Core MVC and Entity Framework Core against a MySQL database. The interesting parts are the business rules — appointments can't overlap on the same vet or pet, pets are blocked after three no-shows for seven days, treatments can only be assigned to completed consultations. I also wired SMTP notifications via MailKit so the clinic gets emails on appointment creation, cancellation, and treatment assignment. The architecture is layered with services for business logic and controllers as thin HTTP handlers. Next steps are unit tests with xUnit and Docker for deployment."

Practice until you can deliver it without notes. The interviewer is judging whether you understand what you built.
</details>

---

## Closing

This study guide covers the breadth of the VetCare project. Use it as a reference while you continue building, as a refresher before interviews, and as a checklist of patterns to apply in your next project.

The project itself is a snapshot — at any point in time it represents your understanding when you wrote it. The exercises are how you find out what you actually know vs what you only think you know. Do them before reading the solutions.

The 90-day roadmap in section 16 is the operational plan. Section 15 is the bug catalog you'll keep referring to when something goes wrong on a future project — the same patterns recur.

Good luck.
