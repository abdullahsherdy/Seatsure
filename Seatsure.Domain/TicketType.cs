using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seatsure.Domain
{
    public class TicketType
    {
       
        public Guid id { get; set; } = new Guid();

        [ForeignKey("Event")] // matching btw the string and classes. 
        // the best practice is to use onModelCreating Configurations 
        // T => t.hasone(t.Event).withMany(e => e.Tickets);
        public Guid Eventid { get; set; }

        // navigational property for one - many relationship btw 
        public Event Event { get; set; }

        [MaxLength(50)]
        public string Title { get; set; }
        // validation in services 

        public decimal Price { get; set; }

        // onCreation, no update 
        public int TotalQuantity { get; set; }

        // onCreation, update every transaction 

        public int AvailableQuantity { get; set; }

        // byteArray 

        public byte[] RowVersion { get; set; }

        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    }
}
