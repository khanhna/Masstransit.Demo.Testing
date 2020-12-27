using System;

namespace Library.Contract
{
    public interface IReservationRequestedGlobalEvent
    {
        public Guid ReservationId { get; set; }
        public DateTime Timestamp { get; set; }
        public Guid MemberId { get; set; }
        public Guid BookId { get; set; }
    }
}
