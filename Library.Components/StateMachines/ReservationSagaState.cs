using System;
using Automatonymous;

namespace Library.Components.StateMachines
{
    public class ReservationSagaState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public string CurrentState { get; set; }
        public DateTime Created { get; set; }
        public DateTime? Reserved { get; set; }
        public Guid MemberId { get; set; }
        public Guid BookId { get; set; }
        public Guid? ExpirationTokenId { get; set; }
    }
}