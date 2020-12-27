using System;

namespace Library.Contract
{
    public interface IBookReservedGlobalEvent
    {
        public Guid ReservationId { get; set; }
        public DateTime TimeStamp { get; set; }
        public Guid MemberId { get; set; }
        public Guid BookId { get; set; }
    }
}
