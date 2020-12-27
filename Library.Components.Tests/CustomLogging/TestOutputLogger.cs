using Microsoft.Extensions.Logging;
using Serilog.Events;
using System;
using ILogger = Serilog.ILogger;

namespace Library.Components.Tests.CustomLogging
{
    public class TestOutputLogger :
        Microsoft.Extensions.Logging.ILogger
    {
        readonly TestOutputLoggerFactory _factory;

        readonly Func<LogLevel, bool> _filter;
        private readonly ILogger _loggerInstance;
        object _scope;

        public TestOutputLogger(TestOutputLoggerFactory factory, bool enabled, ILogger loggerInstance)
            : this(factory, _ => enabled, loggerInstance)
        {
        }

        public TestOutputLogger(TestOutputLoggerFactory factory, Func<LogLevel, bool> filter, ILogger loggerInstance)
        {
            _factory = factory;
            _filter = filter;
            _loggerInstance = loggerInstance;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            _scope = state;

            return TestDisposable.Instance;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            if (formatter == null)
                throw new ArgumentNullException(nameof(formatter));

            var message = formatter(state, exception);

            if (string.IsNullOrEmpty(message))
                return;

            if (exception != null)
                message += Environment.NewLine + Environment.NewLine + exception;

            _loggerInstance.Write(ConvertSerilogEventLevel(logLevel), exception, message);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None && _filter(logLevel);
        }

        private static LogEventLevel ConvertSerilogEventLevel(LogLevel level) =>
            level switch
            {
                LogLevel.Critical => LogEventLevel.Fatal,
                LogLevel.Error => LogEventLevel.Error,
                LogLevel.Warning => LogEventLevel.Warning,
                LogLevel.Information => LogEventLevel.Information,
                LogLevel.Debug => LogEventLevel.Debug,
                _ => LogEventLevel.Verbose
            };


        internal class TestDisposable : IDisposable
        {
            public static readonly TestDisposable Instance = new TestDisposable();

            public void Dispose()
            {
                // intentionally does nothing
            }
        }
    }
}