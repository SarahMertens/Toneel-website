using System.ComponentModel.DataAnnotations;

namespace Toneel.Core.Reservations;

/// <summary>
/// What a user types into the reservation form. The validation attributes
/// are checked by the form and again by <see cref="ReservationService"/>.
/// </summary>
public sealed class ReservationInput
{
    public const int MaxNameLength = 60;

    [Required(ErrorMessage = "Vul een voornaam in.")]
    [StringLength(MaxNameLength, ErrorMessage = "Gebruik hoogstens 60 tekens.")]
    public string FirstName { get; set; } = "";

    [Required(ErrorMessage = "Vul een achternaam in.")]
    [StringLength(MaxNameLength, ErrorMessage = "Gebruik hoogstens 60 tekens.")]
    public string LastName { get; set; } = "";

    public DateOnly? Date { get; set; }

    [Range(
        Reservation.MinSeats,
        Reservation.MaxSeats,
        ErrorMessage = "Kies een aantal tussen 1 en 100.")]
    public int Seats { get; set; } = Reservation.MinSeats;

    public static ReservationInput From(Reservation reservation) => new()
    {
        FirstName = reservation.FirstName,
        LastName = reservation.LastName,
        Date = reservation.Date,
        Seats = reservation.Seats,
    };
}
