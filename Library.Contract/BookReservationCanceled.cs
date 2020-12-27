using System;

namespace Library.Contract
{
    public interface IBookReservationCanceledGlobalEvent
    {
        public Guid BookId { get; }
        public Guid ReservationId { get; }
    }
}