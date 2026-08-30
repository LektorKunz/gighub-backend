# GigHub.Api - backend

## Sådan kører du det

Forudsætter .NET SDK 10 installeret (`dotnet --version` skal starte med `10.`).

```bash
1. Hent NuGet-pakker (kræver netadgang)
dotnet restore

2. Installér EF Core-værktøjet, hvis det ikke allerede er gjort (én gang pr. maskine)
dotnet tool install --global dotnet-ef

3. Generér migrationen (der findes bevidst ingen Migrations/-mappe i dette repo)
dotnet ef migrations add InitialCreate --project GigHub.Api --startup-project GigHub.Api

4. Opret/opdatér SQLite-databasen (gighub.db) ud fra migrationen
dotnet ef database update --project GigHub.Api --startup-project GigHub.Api

5. Byg hele løsningen
dotnet build

6. Kør de automatiske tests
dotnet test

7. Start API'et
dotnet run --project GigHub.Api
```

