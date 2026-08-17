using System.ComponentModel;

namespace Seatsure.Domain
{
    public class TicketType
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid EventId { get; set; }
        public Event Event { get; set; } = null!;
        public string Name { get; set; } = string.Empty;
        [TypeConverter(typeof(DecimalConverter))]
        public decimal Price { get; set; }
        public int TotalQuantity { get; set; }
        public int AvailableQuantity { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    }
}
