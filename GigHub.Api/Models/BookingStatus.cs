namespace GigHub.Api.Models;

/// <summary>
/// Status for en booking. Sættes af <see cref="GigHub.Api.Services.IBookingService"/> ud fra
/// kapacitets-/venteliste-logikken (forretningsregel 1 i design-brief.md) - klienten kan
/// ikke selv vælge status ved oprettelse.
/// </summary>
public enum BookingStatus
{
    /// <summary>Bekræftet plads til eventet.</summary>
    Booket,

    /// <summary>Eventet var fuldt ved oprettelsen - brugeren rykker op, hvis en Booket-booking aflyses.</summary>
    Venteliste,

    /// <summary>Brugeren har selv aflyst, eller er blevet aflyst.</summary>
    Aflyst
}
