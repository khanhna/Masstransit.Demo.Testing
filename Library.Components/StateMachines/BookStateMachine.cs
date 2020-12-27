using Automatonymous;
using Automatonymous.Binders;
using Library.Contract;
using MassTransit;

namespace Library.Components.StateMachines
{
    // ReSharper disable UnassignedGetOnlyAutoProperty MemberCanBePrivate.Global
    public sealed class BookStateMachine : MassTransitStateMachine<BookSagaState>
    {
        static BookStateMachine()
        {
            MessageContracts.Initialize();
        }

        public BookStateMachine()
        {
            // Instance property specified
            InstanceState(x => x.CurrentState);

            // Event declaring correlation
            Event(() => BookReservationRequested, x => x.CorrelateById(m => m.Message.BookId));

            // Actual processing steps
            Initially(When(BookAdded).CopyDataToInstance().TransitionTo(Available));

            During(Available, 
                When(BookReservationRequested)
                    .PublishAsync(ctx => ctx.Init<IBookReservedGlobalEvent>(new
                    {
                        ReservationId = ctx.Data.ReservationId,
                        TimeStamp = InVar.Timestamp,
                        MemberId = ctx.Data.MemberId,
                        BookId = ctx.Data.BookId
                    }))
                    .TransitionTo(Reserved));

            During(Reserved,
                When(BookReservationCanceled)
                    .TransitionTo(Available));
        }

        public State Available { get; }
        public State Reserved { get; }

        public Event<IBookAddedGlobalEvent> BookAdded { get; }
        public Event<IReservationRequestedGlobalEvent> BookReservationRequested { get; }
        public Event<IBookReservationCanceledGlobalEvent> BookReservationCanceled { get; }
    }

    public static class BookStateMachineExtensions
    {
        public static EventActivityBinder<BookSagaState, IBookAddedGlobalEvent> CopyDataToInstance(
            this EventActivityBinder<BookSagaState, IBookAddedGlobalEvent> binder) => binder.Then(
            x =>
            {
                x.Instance.DateAdded = x.Data.TimeStamp.Date;
                x.Instance.Isbn = x.Data.Isbn;
                x.Instance.Title = x.Data.Title;
            });
    }
}