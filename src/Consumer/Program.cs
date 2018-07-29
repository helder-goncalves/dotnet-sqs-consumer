using System;
using System.IO;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Proto;
using Serilog;
using Consumer.Actors;
using Consumer.Dependencies;
using Consumer.Messages;
using Queueing;
using StructureMap;
using Queueing.Queueing;
using Queueing.Configuration;
using Queueing.Queueing.Models;
using System.Collections.Generic;
using Consumer.Factories;
using Shared;

namespace Consumer
{
    /// <summary>
    /// Application bootstrapper
    /// </summary>
    class Program
    {
        private static Serilog.ILogger _logger;
        private static IConfiguration _configuration;
        private static AutoResetEvent _closing = new AutoResetEvent(false);
        private static CancellationTokenSource _cancellationToken = new CancellationTokenSource();

        /// <summary>
        /// Entry point for the application
        /// </summary>
        /// <returns>A task that completes when the application ends.</returns>
        static void Main()
        {
            _configuration = BuildConfiguration();
            _logger = CreateLogger();

            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            AssemblyLoadContext.Default.Unloading += OnShutdown;
            Console.CancelKeyPress += OnCancelKeyPress;

            try
            {
                _logger.Information("Starting Consumer. Press Ctrl+C to exit.");

                if (IsDevelopmentEnvironment())
                    _logger.Information(_configuration.Dump());

                var resolver = new Container();

                var actorFactory = resolver.GetInstance<IActorFactory>();
                var dequeuer = actorFactory.GetActor<Dequeuer>();

                _closing.WaitOne();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error starting Consumer");
            }
            finally
            {
                Serilog.Log.CloseAndFlush();
            }
        }

        static Props WithReceiveMiddleware(Props props)
        {
            return props.WithReceiveMiddleware(next => async c =>
                {
                    if (c.Message is SendRequest request)
                    {
                        _logger.Information("{ActorId} received {MessageType} for {MessageId}", c.Self.Id, c.Message.GetType().Name, request.MessageId);
                    }
                    if (c.Message is DeleteMessage delete)
                    {
                        _logger.Information("{ActorId} received {MessageType} for {MessageId}", c.Self.Id, c.Message.GetType().Name, delete.MessageId);
                    }
                    else
                    {
                        _logger.Information("{ActorId} received {MessageType}", c.Self.Id, c.Message.GetType().Name);
                    }

                    await next(c);
                    //_logger.Information("Exit {ActorType} received {MessageType}", c.Actor.GetType().Name, c.Message.GetType().Name);
                });
        }

        Serilog.ILogger ConfigureLogger()
        {
            var logger = new LoggerConfiguration()
                .WriteTo.ColoredConsole()
                .CreateLogger();

            return Serilog.Log.Logger = logger;
        }

        static void OnShutdown(AssemblyLoadContext context)
        {
            _logger.Information("Shutting down Consumer");
            _cancellationToken.Cancel();
            Serilog.Log.CloseAndFlush();
        }

        static void OnCancelKeyPress(object sender, ConsoleCancelEventArgs args)
        {
            args.Cancel = true;
            _closing.Set();
            _cancellationToken.Cancel();
        }

        static void OnUnhandledException(object sender, UnhandledExceptionEventArgs ex)
        {
            Console.WriteLine(ex.ExceptionObject.ToString());
            Environment.Exit(1);
        }

        /// <summary>
        /// Creates a logger using the application configuration
        /// </summary>
        /// <param name="configuration">The configuration to read from</param>
        /// <returns>An logger instance</returns>
        static Serilog.ILogger CreateLogger()
        {
            var logger = new LoggerConfiguration()
                .ReadFrom.Configuration(_configuration)
                .CreateLogger()
                .ForContext<Program>();

            return Serilog.Log.Logger = logger;
        }

        /// <summary>
        /// Builds a configuration from file and event variable sources
        /// </summary>
        /// <returns>The built configuration</returns>
        static IConfiguration BuildConfiguration()
        {
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.local.json", optional: true)
                .AddEnvironmentVariables(prefix: "Consumer_")
                .Build();
        }

        /// <summary>
        /// Determines whether the application is running in Development mode
        /// </summary>
        /// <returns>True if running in Development, otherwise False</returns>
        static bool IsDevelopmentEnvironment()
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            return "Development".Equals(environment, StringComparison.OrdinalIgnoreCase);
        }

        private static IAmazonSQS CreateAmazonSQSClient(QueueSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.Endpoint))
                return new AmazonSQSClient();

            // localstack usage
            var config = new AmazonSQSConfig { ServiceURL = settings.Endpoint };
            return new AmazonSQSClient(config);
        }
    }
}