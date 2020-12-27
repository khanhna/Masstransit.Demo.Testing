using MassTransit;
using MassTransit.Topology.Topologies;

namespace Library.Contract
{
    public class MessageContracts
    {
        static bool _initialized;

        public static void Initialize()
        {
            if(_initialized)
                return;

            GlobalTopology.Send.UseCorrelationId<IBookAddedGlobalEvent>(x => x.BookId);
            GlobalTopology.Send.UseCorrelationId<IBookReservationCanceledGlobalEvent>(x => x.BookId);
            GlobalTopology.Send.UseCorrelationId<IReservationRequestedGlobalEvent>(x => x.ReservationId);
            GlobalTopology.Send.UseCorrelationId<IReservationExpiredGlobalEvent>(x => x.ReservationId);

            _initialized = true;
        }
    }
}