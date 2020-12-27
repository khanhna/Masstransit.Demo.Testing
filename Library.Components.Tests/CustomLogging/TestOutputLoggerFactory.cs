using Microsoft.Extensions.Logging;
using ILogger = Serilog.ILogger;

namespace Library.Components.Tests.CustomLogging
{
    public class TestOutputLoggerFactory :
        ILoggerFactory
    {
        readonly bool _enabled;
        private readonly ILogger _logger;

        public TestOutputLoggerFactory(bool enabled, ILogger logger)
        {
            _enabled = enabled;
            _logger = logger;
        }

        public Microsoft.Extensions.Logging.ILogger CreateLogger(string name)
        {
            return new TestOutputLogger(this, _enabled, _logger);
        }

        public void AddProvider(ILoggerProvider provider)
        {
        }

        public void Dispose()
        {
        }
    }
}