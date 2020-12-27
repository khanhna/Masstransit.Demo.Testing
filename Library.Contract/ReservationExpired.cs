using System;

namespace Library.Contract
{
    public interface IReservationExpiredGlobalEvent
    {
        public Guid ReservationId { get; }
    }
}
