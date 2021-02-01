using Microsoft.Extensions.Logging;
using System;
using static System.Console;

namespace CS7
{
    public class DostawcaProtokoluKonsoli : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName)
        {
            return new ProtokoluKonsoli();
        }

        public void Dispose()
        {

        }

    }

    public class ProtokoluKonsoli : ILogger
    {
        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Trace:
                case LogLevel.Information:
                case LogLevel.None:
                    return false;

                case LogLevel.Debug:
                case LogLevel.Error:
                case LogLevel.Warning:
                case LogLevel.Critical:
                default:
                    return true;

            }
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (eventId.Id == 200100)
            {
                System.Console.WriteLine($"Poziom: {logLevel}, ID zdarzenia: {eventId.Id}");

                if (state != null)
                {
                    System.Console.WriteLine($"Stan: {state}");
                }

                if (exception != null)
                {
                    System.Console.WriteLine($"Wyjatek: {exception}");
                }

                System.Console.WriteLine();
            }
        }

    }
}