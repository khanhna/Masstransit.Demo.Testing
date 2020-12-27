using FluentAssertions;
using Library.Components.StateMachines;
using Library.Contract;
using MassTransit;
using MassTransit.Testing;
using System.Threading.Tasks;
using Serilog;
using Serilog.Events;
using Xunit;
using Xunit.Abstractions;

namespace Library.Components.Tests
{
    public class When_A_Book_Is_Added : StateMachineTestFixture<BookStateMachine, BookSagaState>,
        IClassFixture<StateMachineTestFixture<BookStateMachine, BookSagaState>>
    {
        public When_A_Book_Is_Added(ITestOutputHelper testOutputHelper)
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
            var bookId = NewId.NextGuid();

            await TestHarness.Bus.Publish<IBookAddedGlobalEvent>(new
            {
                BookId = bookId,
                Isbn = "0307969959",
                Title = "Neuromancer"
            });

            (await TestHarness.Consumed.Any<IBookAddedGlobalEvent>()).Should()
                .BeTrue($"The {nameof(IBookAddedGlobalEvent)} should be consumed by Masstransit!");
            (await SagaHarness.Consumed.Any<IBookAddedGlobalEvent>()).Should()
                .BeTrue($"The {nameof(IBookAddedGlobalEvent)} should be consumed by Saga!");
            (await SagaHarness.Created.Any(x => x.CorrelationId == bookId)).Should()
                .BeTrue("The correlationId of the book should be match!");
            SagaHarness.Created.ContainsInState(bookId, Machine, Machine.Available).Should()
                .NotBeNull("Saga instance of the book should be exists!");
            (await SagaHarness.Exists(bookId, x => x.Available)).HasValue.Should()
                .BeTrue("Saga instance should exists!");

            Output.Information("Ran in here");
        }
    }
}
