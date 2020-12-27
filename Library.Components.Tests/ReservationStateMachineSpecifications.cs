using FluentAssertions;
using Library.Components.StateMachines;
using Library.Contract;
using MassTransit;
using MassTransit.ExtensionsDependencyInjectionIntegration;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Library.Components.Tests
{
    public class When_A_Reservation_Is_Added :
        StateMachineTestFixture<ReservationStateMachine, ReservationSagaState>,
        IClassFixture<StateMachineTestFixture<ReservationStateMachine, ReservationSagaState>>
    {
        public When_A_Reservation_Is_Added(ITestOutputHelper testOutputHelper)
        {
            Output = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.TestOutput(testOutputHelper, LogEventLevel.Debug)
                .CreateLogger()
                .ForContext<When_A_Book_Is_Added>();

            ConfigureLogging(Output);
        }

        [Fact]
        public async Task Should_Create_A_Saga_Instance()
        {
            var reservationId = NewId.NextGuid();
            var bookId = NewId.NextGuid();
            var memberId = NewId.NextGuid();

            await TestHarness.Bus.Publish<IReservationRequestedGlobalEvent>(new
            {
                ReservationId = reservationId,
                Timestamp = InVar.Timestamp,
                MemberId = memberId,
                BookId = bookId
            });

            (await TestHarness.Consumed.Any<IReservationRequestedGlobalEvent>()).Should()
                .BeTrue("Message should be consumed by normal bus!");
            (await SagaHarness.Consumed.Any<IReservationRequestedGlobalEvent>()).Should()
                .BeTrue("Message should be consumed by saga!");
            //(await SagaHarness.Created.Any(x => x.CorrelationId == reservationId)).Should()
            //    .BeTrue("A reservation saga state machine instance should be created!");
            SagaHarness.Created.ContainsInState(reservationId, Machine, Machine.Requested).Should()
                .NotBeNull("A reservation saga state machine instance should be created!");
            (await SagaHarness.Exists(reservationId, x => x.Requested)).HasValue.Should()
                .BeTrue("Reservation saga instance should exist!");
        }
    }

    public class When_A_Book_Reservation_Is_Requested_For_An_Available_Book :
        StateMachineTestFixture<ReservationStateMachine, ReservationSagaState>,
        IClassFixture<StateMachineTestFixture<ReservationStateMachine, ReservationSagaState>>
    {
        private IStateMachineSagaTestHarness<BookSagaState, BookStateMachine> _bookSagaHarness;
        private BookStateMachine _bookMachine;

        public When_A_Book_Reservation_Is_Requested_For_An_Available_Book(ITestOutputHelper testOutputHelper)
        {
            Output = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.TestOutput(testOutputHelper, LogEventLevel.Debug)
                .CreateLogger()
                .ForContext<When_A_Book_Is_Added>();

            ConfigureLogging(Output);
        }

        protected override void ConfigureMassTransit(IServiceCollectionBusConfigurator configurator)
        {
            configurator.AddSagaStateMachine<BookStateMachine, BookSagaState>().InMemoryRepository();
            configurator.AddSagaStateMachineTestHarness<BookStateMachine, BookSagaState>();
        }

        protected override void ConfigureService(IServiceCollection collection)
        {
            _bookSagaHarness =
                Provider.GetRequiredService<IStateMachineSagaTestHarness<BookSagaState, BookStateMachine>>();
            _bookMachine = Provider.GetRequiredService<BookStateMachine>();
        }

        [Fact]
        public async Task Should_Reserved_The_Book()
        {
            var reservationId = NewId.NextGuid();
            var bookId = NewId.NextGuid();
            var memberId = NewId.NextGuid();

            await TestHarness.Bus.Publish<IBookAddedGlobalEvent>(new
            {
                BookId = bookId,
                Isbn = "0307969959",
                Title = "Neuromancer"
            });

            var existsId = await _bookSagaHarness.Exists(bookId, x => x.Available);
            existsId.HasValue.Should().BeTrue("Book saga state instance should exists!");

            await TestHarness.Bus.Publish<IReservationRequestedGlobalEvent>(new
            {
                ReservationId = reservationId,
                Timestamp = InVar.Timestamp,
                MemberId = memberId,
                BookId = bookId
            });

            (await SagaHarness.Consumed.Any<IReservationRequestedGlobalEvent>()).Should()
                .BeTrue("Message should be consumed by Reservation Saga!");
            (await _bookSagaHarness.Consumed.Any<IReservationRequestedGlobalEvent>()).Should()
                .BeTrue("Message should be consumed by Book Saga!");

            existsId = await SagaHarness.Exists(reservationId, x => x.Reserved);
            existsId.HasValue.Should().BeTrue("Reservation saga state instance should exists!");

            SagaHarness.Sagas.ContainsInState(reservationId, Machine, x => x.Reserved).Should()
                .NotBeNull("Reservation saga state instance should exists on [Reserved] state!");
        }
    }

    public class When_A_Reservation_Expired :
        StateMachineTestFixture<ReservationStateMachine, ReservationSagaState>,
        IClassFixture<StateMachineTestFixture<ReservationStateMachine, ReservationSagaState>>
    {
        private IStateMachineSagaTestHarness<BookSagaState, BookStateMachine> _bookSagaHarness;
        private BookStateMachine _bookMachine;

        public When_A_Reservation_Expired(ITestOutputHelper testOutputHelper)
        {
            Output = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.TestOutput(testOutputHelper, LogEventLevel.Debug)
                .CreateLogger()
                .ForContext<When_A_Book_Is_Added>();

            ConfigureLogging(Output);
        }

        protected override void ConfigureMassTransit(IServiceCollectionBusConfigurator configurator)
        {
            configurator.AddSagaStateMachine<BookStateMachine, BookSagaState>().InMemoryRepository();
            configurator.AddSagaStateMachineTestHarness<BookStateMachine, BookSagaState>();
        }

        protected override void ConfigureService(IServiceCollection collection)
        {
            _bookSagaHarness =
                Provider.GetRequiredService<IStateMachineSagaTestHarness<BookSagaState, BookStateMachine>>();
            _bookMachine = Provider.GetRequiredService<BookStateMachine>();
        }

        [Fact(Skip = "Sometime bug | This one must run separately in order to success!")]
        public async Task Should_Mark_Book_As_Available()
        {
            var reservationId = NewId.NextGuid();
            var bookId = NewId.NextGuid();
            var memberId = NewId.NextGuid();

            await TestHarness.Bus.Publish<IBookAddedGlobalEvent>(new
            {
                BookId = bookId,
                Isbn = "0307969959",
                Title = "Neuromancer"
            });

            (await _bookSagaHarness.Exists(bookId, x => x.Available)).HasValue.Should()
                .BeTrue("Book Saga state machine should be existed at Available State!");

            await TestHarness.Bus.Publish<IReservationRequestedGlobalEvent>(new
            {
                ReservationId = reservationId,
                Timestamp = InVar.Timestamp,
                MemberId = memberId,
                BookId = bookId
            });

            (await SagaHarness.Exists(reservationId, x => x.Reserved)).HasValue.Should()
                .BeTrue("Reservation Saga state machine should be existed at Reserved State!");
            (await _bookSagaHarness.Exists(bookId, x => x.Reserved)).HasValue.Should()
                .BeTrue("Book Saga state machine should be existed at Reserved State!");

            // Turn the clock toward 24 hours
            await AdvanceSystemTime(TimeSpan.FromHours(24));

            (await SagaHarness.NotExists(reservationId)).HasValue.Should().BeFalse(
                "The Reservation state machine should be finalized already cause of reservation expired itself!");
            (await _bookSagaHarness.Exists(bookId, x => x.Available)).HasValue.Should().BeTrue(
                "Book Saga state machine should be existed at Available State cause the reservation was expired!");
        }
    }
}
