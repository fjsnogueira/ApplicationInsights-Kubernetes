﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Kubernetes.Debugging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BasicConsoleAppILogger
{
    class Program
    {
        static void Main(string[] args)
        {
            // Create the DI container.
            IServiceCollection services = new ServiceCollection();

            // Uncomment the following lines for debugging AI.K8s.
            // Refer to https://github.com/microsoft/ApplicationInsights-Kubernetes/blob/develop/docs/SelfDiagnostics.MD for details.
            // var observer = new ApplicationInsightsKubernetesDiagnosticObserver(DiagnosticLogLevel.Trace);
            // ApplicationInsightsKubernetesDiagnosticSource.Instance.Observable.SubscribeWithAdapter(observer);

            // Add application insights for Kubernetes. Making sure this is called before services.Configure<TelemetryConfiguration>().
            services.AddApplicationInsightsKubernetesEnricher();

            // Channel is explicitly configured to do flush on it later.
            var channel = new InMemoryChannel();
            services.Configure<TelemetryConfiguration>(
                (config) =>
                {
                    config.TelemetryChannel = channel;
                }
            );

            // Add the logging pipelines to use. We are using Application Insights only here.
            services.AddLogging(builder =>
            {
                // Optional: Apply filters to configure LogLevel Trace or above is sent to
                // Application Insights for all categories.
                builder.AddFilter<Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider>("", LogLevel.Trace);
                builder.AddApplicationInsights("---Your AI instrumentation key---");

                // Optional: Show the logs in console at the same time
                builder.AddConsole();
            });

            // Build ServiceProvider.
            IServiceProvider serviceProvider = services.BuildServiceProvider();
            ILogger<Program> logger = serviceProvider.GetRequiredService<ILogger<Program>>();

            while (true)
            {
                // Begin a new scope. This is optional.
                using (logger.BeginScope(new Dictionary<string, object> { { "Method", nameof(Main) } }))
                {
                    logger.LogInformation("Logger is working"); // this will be captured by Application Insights.
                }

                // Explicitly call Flush() followed by sleep is required in Console Apps.
                // This is to ensure that even if application terminates, telemetry is sent to the back-end.
                channel.Flush();
                Thread.Sleep(10000);
            }
        }
    }
}
