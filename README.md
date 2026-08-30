# GigHub.Api — facit-backend

> ## ⚠️ VIGTIGT: koden er IKKE compilet i det miljø, den blev skrevet i
>
> Dette sandbox-miljø har ikke adgang til NuGet (`api.nuget.org` er blokeret af en proxy), så
> koden har **ikke** kunnet køres igennem `dotnet restore`, `dotnet build` eller `dotnet test`.
> Den er skrevet omhyggeligt ud fra kendskab til .NET 10 / ASP.NET Core 10 / EF Core 10-API'erne,
> men **underviseren skal selv køre nedenstående, før koden bruges i undervisningen**:
>
> ```bash
> cd facit-backend
> dotnet restore
> dotnet ef migrations add InitialCreate --project GigHub.Api --startup-project GigHub.Api
> dotnet ef database update --project GigHub.Api --startup-project GigHub.Api
> dotnet build
> dotnet test
> dotnet run --project GigHub.Api
> ```
>
> Ting, der er særligt sandsynlige at skulle rettes efter en `dotnet restore`:
> - **Pakkeversionerne** i `GigHub.Api/GigHub.Api.csproj` og `GigHub.Api.Tests/GigHub.Api.Tests.csproj`
>   er sat til `10.0.0` (EF Core/ASP.NET Core-pakker) og bedste-gæt-versioner for
>   `Scalar.AspNetCore` og xUnit-værktøjskæden. Kør `dotnet restore` og ret version op/ned,
>   hvis NuGet foreslår noget andet.
> - Se den fulde liste over ikke-verificerede punkter nederst i denne fil.

## Hvad er dette?

Facit-kode (model-løsning) til GigHub-casen i "Programmering, 3. semester". Repræsenterer
**sluttilstanden efter gang 08 (uge 44)** — dvs. alle features fra gang 01 til og med gang 08
er med. Gang 09 og 10 tilføjer ingen nye endpoints (kun tests, refactoring og aflevering, se
`design-brief.md` afsnit 4), så der er ikke noget API-mæssigt at bygge videre på for de gange —
til gengæld er selve `GigHub.Api.Tests`-projektet her et eksempel på, hvad gang 09 arbejder med.

Der findes bevidst ingen håndskrevet `Migrations/`-mappe i dette repo — auto-genereret EF Core-kode
er for let at få subtilt forkert i hånden. Kør `dotnet ef migrations add InitialCreate` selv
(se ovenfor) for at generere den.

**Port:** `GigHub.Api/Properties/launchSettings.json` er tilføjet efter at facit-koden blev
skrevet (den manglede i det oprindelige udkast) og fastlåser porten til `https://localhost:5001`
(og `http://localhost:5000`), så den matcher `apiUrl` i `facit-frontend/src/environments/`.
Kør `dotnet run --project GigHub.Api --launch-profile https` for at bruge den. Denne fil er
ikke omfattet af NuGet-forbeholdet ovenfor — den er ren JSON og kræver ikke restore.

## Filoversigt pr. undervisningsgang

Denne tabel viser, hvilken gang der (i den rigtige, trinvise undervisning) introducerer hvilken
fil/feature. Selve koden i dette repo er ikke opdelt i gang-for-gang-lag — den er den samlede
sluttilstand — men kommentarerne i hver fil refererer til den gang, hvor featuren hører hjemme.

| Gang | Uge | Nyt i API'et (design-brief.md afsnit 4) | Relevante filer i dette repo |
|---|---|---|---|
| 01 | 36 | `GET /api/events`, `GET /api/events/{id}` — hardcodet `List<Event>`, ingen DB | `Controllers/EventsController.cs` (i den rigtige gang 01-øvelse er dette en meget simplere, hardcodet version — her vises sluttilstanden med EF Core) |
| 02 | 37 | Samme endpoints flyttet til rigtig `EventsController`, CORS, Angular forbindes | `Program.cs` (CORS-policy `"AngularDev"`) |
| 03 | 38 | EF Core + SQLite, `Event` bliver rigtig tabel, migration, seed-data | `Data/GighubDbContext.cs`, `Models/Event.cs`, `Data/DbSeeder.cs`, `appsettings.json` (`ConnectionStrings:DefaultConnection`) |
| 04 | 39 | `POST/PUT/DELETE /api/events`, DTOs + validering, `POST /api/events/{id}/bookings` (fake-bruger) | `Dtos/EventDtos.cs`, `Controllers/EventsController.cs` (Create/Update/Delete), `Models/Booking.cs`, `Controllers/BookingsController.cs` — OBS: denne facit-version viser allerede gang 06's refactor (ægte JWT-bruger, intet `UserId` i body) |
| 05 | 41 | Kapacitet/venteliste-logik i `IBookingService`, `GET /api/events?genre=&search=&page=&pageSize=` | `Services/IBookingService.cs`, `Services/BookingService.cs`, `Dtos/PagedResult.cs`, `EventsController.GetEvents` |
| 06 | 42 | `POST /api/auth/register`, `POST /api/auth/login` (JWT), `[Authorize]`, roller, booking bruger ægte `UserId` fra token | `Services/IAuthService.cs`, `Services/AuthService.cs`, `Controllers/AuthController.cs`, `Dtos/AuthDtos.cs`, `Common/ClaimsPrincipalExtensions.cs`, JWT-opsætning i `Program.cs` |
| 07 | 43 | `POST /api/events/{id}/reviews` + forretningsregel, global fejlhåndtering (`ProblemDetails`), `GET /api/events/{id}` inkl. gennemsnitsrating | `Services/IReviewService.cs`, `Services/ReviewService.cs`, `Controllers/ReviewsController.cs`, `Middleware/ExceptionHandlingMiddleware.cs`, `Common/Exceptions/AppExceptions.cs`, `EventsController.GetEvent` (BookedCount/AverageRating) |
| 08 | 44 | `POST/DELETE /api/events/{id}/favorites`, `POST /api/events/{id}/image` (filupload) | `Models/Favorite.cs`, `Controllers/FavoritesController.cs`, `Dtos/FavoriteDtos.cs`, `EventsController.UploadImage`, `wwwroot/uploads/events/` |
| 09 | 46 | Ingen nye endpoints — xUnit-tests af `IBookingService`, refactoring | `GigHub.Api.Tests/` (hele projektet), særligt `BookingServiceTests.cs` |
| 10 | 47 | Ingen nye endpoints — finpudsning, aflevering | — (ingen ny kode i dette repo) |

**Understøttende læse-endpoints ud over tabellen ovenfor** (tilføjet af pragmatiske grunde, så
Angular-komponenterne fra design-brief.md afsnit 4 rent faktisk kan fungere — ikke eksplicit
nævnt i endpoint-tabellen, men naturlige følgesvende til den):

- `GET /api/bookings/mine` (`BookingsController`) — bruges af `BookingButtonComponent` (gang 04/06) til at vise "du er allerede booket/på venteliste".
- `GET /api/events/{id}/reviews` (`ReviewsController`) — bruges af `ReviewListComponent` (gang 07).
- `GET /api/favorites` (`FavoritesController`) — bruges af favorit-hjerte-knappen (gang 08) til at vise sin initiale state.

## Projektstruktur

```
facit-backend/
├── GigHub.slnx                     (løsningsfil i .NET 10's nye .slnx-format)
├── GigHub.Api/
│   ├── GigHub.Api.csproj
│   ├── Program.cs                  (service-registrering + middleware-pipeline)
│   ├── appsettings.json            (SQLite connection string, JWT-nøgle)
│   ├── Models/                     (User, Event, Booking, Review, Favorite + enums)
│   ├── Data/                       (GighubDbContext, DbSeeder)
│   ├── Dtos/                       (EF-entiteter eksponeres aldrig direkte i API'et)
│   ├── Services/                   (IBookingService, IAuthService, IReviewService + implementationer)
│   ├── Controllers/                (EventsController, BookingsController, AuthController, ReviewsController, FavoritesController)
│   ├── Middleware/                 (ExceptionHandlingMiddleware → ProblemDetails)
│   ├── Common/                     (ClaimsPrincipalExtensions, custom exceptions)
│   └── wwwroot/uploads/events/     (uploadede eventbilleder havner her, gang 08)
└── GigHub.Api.Tests/
    ├── GigHub.Api.Tests.csproj
    └── BookingServiceTests.cs      (xUnit + EF Core InMemory, matcher gang 09)
```

## Sådan kører du det

Forudsætter .NET SDK 10 installeret (`dotnet --version` skal starte med `10.`).

```bash
cd facit-backend

# 1. Hent NuGet-pakker (kræver netadgang - virker IKKE i det sandbox-miljø, koden blev skrevet i)
dotnet restore

# 2. Installér EF Core-værktøjet, hvis det ikke allerede er gjort (én gang pr. maskine)
dotnet tool install --global dotnet-ef

# 3. Generér migrationen (der findes bevidst ingen Migrations/-mappe i dette repo, se ovenfor)
dotnet ef migrations add InitialCreate --project GigHub.Api --startup-project GigHub.Api

# 4. Opret/opdatér SQLite-databasen (gighub.db) ud fra migrationen
dotnet ef database update --project GigHub.Api --startup-project GigHub.Api

# 5. Byg hele løsningen
dotnet build

# 6. Kør de automatiske tests
dotnet test

# 7. Start API'et
dotnet run --project GigHub.Api
```

Når API'et kører i Development-miljø:

- **Scalar** (interaktiv API-dokumentation/test-UI) er tilgængelig på `/scalar/v1`.
- Databasen seedes automatisk med testdata (se `Data/DbSeeder.cs`) første gang, `Users`-tabellen
  er tom — 5 brugere (1 admin, 2 arrangører, 2 deltagere, alle med adgangskoden
  `Password123!`, se `DbSeeder.SeedUserPassword`) og 4 events.
- CORS er kun åbnet for `http://localhost:4200` (Angular CLI's dev-server-port).

**OBS om `Jwt:Key` i `appsettings.json`:** den er sat til en placeholder-værdi
(`CHANGE_ME_...`). Det er fint til lokal undervisningsbrug, men skal **ikke** committes med en
rigtig, hemmelig værdi — brug `dotnet user-secrets` eller en miljøvariabel, hvis dette nogensinde
skal køre uden for en lokal maskine.

## Alt, der IKKE er verificeret, og som bør tjekkes

Fordi koden ikke har kunnet compiles i skrivende miljø, er følgende **ikke** bekræftet korrekt —
tjek dem efter `dotnet restore && dotnet build && dotnet test`:

1. **Alle NuGet-pakkeversioner.** Sat til `10.0.0` for EF Core/ASP.NET Core-pakkerne (rimeligt
   gæt, følger SDK-versionen), og til bedste-gæt-versioner for `Scalar.AspNetCore` (`2.1.13`) og
   xUnit-værktøjskæden (`Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio`,
   `coverlet.collector`) i `GigHub.Api.Tests.csproj`. Disse pakker versioneres uafhængigt af
   .NET/EF Core og kan sagtens være forældede i skrivende stund.
2. **Al C#-syntaks og alle API-signaturer** — herunder `TokenValidationParameters`,
   `JwtSecurityToken`, `PasswordHasher<TUser>`, `EF.Functions.Like`, `Database.IsRelational()`,
   `Database.BeginTransactionAsync(IsolationLevel, ...)`, `ModelBuilder`-konfigurationen i
   `GighubDbContext`, `AddOpenApi()`/`MapOpenApi()`/`MapScalarApiReference()` og record-DTO'er med
   `[property: ...]`-attributter. Alt er skrevet ud fra kendskab til API'erne, men intet er kørt.
3. **Transaktionslogikken i `BookingService.CreateBookingAsync`** (kapacitets-/ventelisteregel,
   forretningsregel 1) — særligt at `IsolationLevel.Serializable` reelt accepteres af
   Microsoft.Data.Sqlite, og at `Database.IsRelational()`-genvejen for InMemory-provideren
   (brugt i testene) opfører sig som forventet.
4. **Unik-constraint-detektionen** i `BookingService.IsUniqueConstraintViolation` (tekstmatch på
   `"UNIQUE constraint failed"`) — den nøjagtige fejlbesked fra Microsoft.Data.Sqlite er ikke
   verificeret i dette miljø.
5. **`dotnet ef migrations add InitialCreate`** er slet ikke kørt (ingen NuGet-adgang) — det er
   selve migrationens genererede kode, der reelt tester, om `Models/` og
   `GighubDbContext.OnModelCreating` er konsistente.
6. **De 7 xUnit-tests i `BookingServiceTests.cs`** er skrevet til at compile og give mening
   logisk, men er aldrig kørt — kør `dotnet test` og bekræft, at de rent faktisk er grønne.
7. **`.slnx`-løsningsfilen** — genereret af `dotnet new sln`/`dotnet sln add` i dette miljø
   (bekræftet at parse korrekt af SDK'en), men selve build/restore af de refererede projekter
   er, som nævnt, ikke gennemført.
