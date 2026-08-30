namespace GigHub.Api.Models;

/// <summary>
/// Brugerroller i GigHub. Gemmes i databasen som streng (se GighubDbContext.OnModelCreating),
/// ikke som int - det gør data læsbare direkte i SQLite, og betyder at man kan omrokere
/// rækkefølgen af enum-værdierne uden at ødelægge eksisterende data.
/// </summary>
public enum UserRole
{
    /// <summary>Almindelig bruger - kan booke, anmelde og favoritmarkere events.</summary>
    Deltager,

    /// <summary>Kan oprette og administrere sine egne events.</summary>
    Arrangoer,

    /// <summary>Kan administrere alle events, uanset hvem der har oprettet dem.</summary>
    Admin
}
