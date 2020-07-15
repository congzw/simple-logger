﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LogCenter.Client
{
    public class RemoteLogger : ILogger
    {
        public string Category { get; set; }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var category = Category ?? "LogCenter.Web.Hubs.RemoteLogger";
            var msg = $"[{logLevel}] {formatter(state, exception)}";
            var reportLogArgs = ReportLogArgs.Create(category, msg, (int)logLevel);

            var reporter = RemoteHubReporter.Instance;
            reporter.ReportLog(reportLogArgs);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public void Dispose()
        {
        }
    }

    [ProviderAlias("RemoteLogCenter")]
    public class RemoteLoggerProvider : ILoggerProvider
    {
        public IDictionary<string, RemoteLogger> RemoteLoggers { get; set; } = new ConcurrentDictionary<string, RemoteLogger>(StringComparer.OrdinalIgnoreCase);

        public ILogger CreateLogger(string categoryName)
        {
            if (!RemoteLoggers.ContainsKey(categoryName))
            {
                RemoteLoggers.Add(categoryName, new RemoteLogger() { Category = categoryName });
            }
            return RemoteLoggers[categoryName];
        }

        public void Dispose()
        {
        }
    }
}
