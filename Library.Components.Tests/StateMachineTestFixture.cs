using Automatonymous;
using Library.Components.Tests.CustomLogging;
using MassTransit;
using MassTransit.Context;
using MassTransit.ExtensionsDependencyInjectionIntegration;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Impl;
using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Xunit;
using ILogger = Serilog.ILogger;

namespace Library.Components.Tests
{
    public class StateMachineTestFixture<TStateMachine, TInstance> : IAsyncLifetime
        where TStateMachine : class, SagaStateMachine<TInstance>
        where TInstance : class, SagaStateMachineInstance
    {
        Task<IScheduler> _scheduler;
        TimeSpan _testOffset;
        protected TStateMachine Machine;
        protected ServiceProvider Provider;
        protected IStateMachineSagaTestHarness<TInstance, TStateMachine> SagaHarness;
        protected InMemoryTestHarness TestHarness;
        protected ILogger Output;

        public async Task InitializeAsync()
        {
            InterceptQuartzSystemTime();

            var collection = new ServiceCollection()
                .AddMassTransitInMemoryTestHarness(cfg =>
                {
                    cfg.AddSagaStateMachine<TStateMachine, TInstance>().InMemoryRepository();
                    cfg.AddPublishMessageScheduler();
                    cfg.AddSagaStateMachineTestHarness<TStateMachine, TInstance>();

                    ConfigureMassTransit(cfg);
                });

            Provider = collection.BuildServiceProvider(true);
            ConfigureService(collection);

            TestHarness = Provider.GetRequiredService<InMemoryTestHarness>();
            TestHarness.OnConfigureInMemoryBus += configurator =>
            {
                var nvc = new NameValueCollection { ["quartz.scheduler.instanceName"] = Guid.NewGuid().ToString() };
                ISchedulerFactory schedulerFactory = new StdSchedulerFactory(nvc);
                configurator.UseInMemoryScheduler(schedulerFactory, out _scheduler);
            };
            await TestHarness.Start();

            SagaHarness = Provider.GetRequiredService<IStateMachineSagaTestHarness<TInstance, TStateMachine>>();
            Machine = Provider.GetRequiredService<TStateMachine>();
        }

        public async Task DisposeAsync()
        {
            try
            {
                await TestHarness.Stop();
            }
            finally
            {
                await Provider.DisposeAsync();
            }

            RestoreDefaultQuartzSystemTime();
        }

        protected virtual void ConfigureMassTransit(IServiceCollectionBusConfigurator configurator)
        {
        }

        protected virtual void ConfigureService(IServiceCollection collection)
        {
        }

        protected async Task AdvanceSystemTime(TimeSpan duration)
        {
            if (duration <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(duration));

            var scheduler = await _scheduler.ConfigureAwait(false);

            await scheduler.Standby().ConfigureAwait(false);

            _testOffset += duration;

            await scheduler.Start().ConfigureAwait(false);
        }

        protected void ConfigureLogging(ILogger logger)
        {
            var loggerFactory = new TestOutputLoggerFactory(true, logger);

            LogContext.ConfigureCurrentLogContext(loggerFactory);
            Quartz.Logging.LogContext.SetCurrentLogProvider(loggerFactory);
        }

        private void InterceptQuartzSystemTime()
        {
            SystemTime.UtcNow = GetUtcNow;
            SystemTime.Now = GetNow;
        }

        private static void RestoreDefaultQuartzSystemTime()
        {
            SystemTime.UtcNow = () => DateTimeOffset.UtcNow;
            SystemTime.Now = () => DateTimeOffset.Now;
        }

        private DateTimeOffset GetUtcNow() => DateTimeOffset.UtcNow + _testOffset;

        private DateTimeOffset GetNow() => DateTimeOffset.Now + _testOffset;

    }
}