namespace Seatsure.Domain
{
    public class Event
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid OrganizerId { get; set; }
        public User Organizer { get; set; } = null!;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string VenueName { get; set; } = string.Empty;
        public DateTime StartsAtUtc { get; set; }
        public EventStatus Status { get; set; } = EventStatus.Draft;
        public DateTime CreatedAtUtc { get; set; }
        public ICollection<TicketType> TicketTypes { get; set; } = new List<TicketType>();
    }
}
