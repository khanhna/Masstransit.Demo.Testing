using System;
using Automatonymous;
using Library.Contract;
using MassTransit;

namespace Library.Components.StateMachines
{
    // ReSharper disable UnassignedGetOnlyAutoProperty MemberCanBePrivate.Global
    public class ReservationStateMachine : MassTransitStateMachine<ReservationSagaState>
    {
        static ReservationStateMachine()
        {
            MessageContracts.Initialize();
        }

        public ReservationStateMachine()
        {
            // Instance property specified
            InstanceState(x => x.CurrentState);

            // Event declaring correlation
            Event(() => BookReserved, x => x.CorrelateById(m => m.Message.ReservationId));
            
            // Scheduling declaring expiration TokenId
            Schedule(() => ExpirationSchedule, x => x.ExpirationTokenId, x => x.Delay = TimeSpan.FromHours(24));

            // Actual processing steps
            Initially(
                When(ReservationRequested)
                    .Then(ctx =>
                    {
                        ctx.Instance.Created = ctx.Data.Timestamp;
                        ctx.Instance.BookId = ctx.Data.BookId;
                        ctx.Instance.MemberId = ctx.Data.MemberId;
                    })
                    .TransitionTo(Requested));

            During(Requested, 
                When(BookReserved)
                    .Then(ctx => ctx.Instance.Reserved = ctx.Data.TimeStamp)
                    .Schedule(ExpirationSchedule, ctx => ctx.Init<IReservationExpiredGlobalEvent>(new {ctx.Data.ReservationId}))
                    .TransitionTo(Reserved));

            During(Reserved,
                When(ReservationExpired)
                    .PublishAsync(ctx => ctx.Init<IBookReservationCanceledGlobalEvent>(new
                    {
                        BookId = ctx.Instance.BookId,
                        ReservationId = ctx.Data.ReservationId
                    }))
                    .Finalize());

            SetCompletedWhenFinalized();
        }

        public State Requested { get; }
        public State Reserved { get; }
        public State Expired { get; }

        public Schedule<ReservationSagaState, IReservationExpiredGlobalEvent> ExpirationSchedule { get; }

        public Event<IReservationRequestedGlobalEvent> ReservationRequested { get; }
        public Event<IBookReservedGlobalEvent> BookReserved { get; }
        public Event<IReservationExpiredGlobalEvent> ReservationExpired { get; }
    }
}