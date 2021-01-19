using System;
using Microsoft.Extensions.Logging;

namespace AzureFunctions.AcceptanceTest.Runner
{
    public static class TestLogger
    {
        public static ILogger Create()
        {
            var logger = new ConsoleUnitLogger();
            return logger;
        }

        class ConsoleUnitLogger : ILogger, IDisposable
        {
            private readonly Action<string> output = Console.WriteLine;

            public void Dispose()
            {
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
                Func<TState, Exception, string> formatter) => output(formatter(state, exception));

            public bool IsEnabled(LogLevel logLevel) => true;

            public IDisposable BeginScope<TState>(TState state) => this;
        }
    }
}