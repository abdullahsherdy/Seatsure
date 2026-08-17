namespace Seatsure.Domain
{
    public class Reservation
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TicketTypeId { get; set; }
        public Guid UserId { get; set; }
        public TicketType TicketType { get; set; } = null!;
        public User User { get; set; } = null!;
        public int Quantity { get; set; }
        public ReservationStatus Status { get; set; }
        public DateTime HoldExpiresAtUtc { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? ConfirmedAtUtc { get; set; }
    }
}
