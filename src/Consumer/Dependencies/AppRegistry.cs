using System;
using System.IO;
using System.Net.Http;
using Amazon.SQS;
using Microsoft.Extensions.Configuration;
using Proto;
using Serilog;
using Consumer.Actors;
using StructureMap;
using Queueing;
using Queueing.Queueing.Models;
using Queueing.Configuration;
using Shared;

namespace Consumer.Dependencies
{
    /// <summary>
    /// Registry of application dependencies used to configure StructureMap containers
    /// </summary>
    public class AppRegistry : Registry
    {
        /// <summary>
        /// Creates a new instance of the <see cref="AppRegistry"/>
        /// </summary>
        /// <param name="configuration">The application configuration to be registered with the container</param>
        /// <param name="logger">The application logger to be registered with the container</param>
        public AppRegistry(IConfiguration configuration, ILogger logger)
        {
            ForSingletonOf<IConfiguration>()
                .Use(configuration);

            // Binds the "Settings" section from appsettings.json to AppSettings
            ForSingletonOf<QueueSettings>()
                .Use(configuration.BindTo<QueueSettings>("Settings"));

            // Automatically sets the Serilog source context to the requesting type
            ForSingletonOf<ILogger>()
                .Use(ctx => logger.ForContext(ctx.ParentType ?? ctx.RootType));

            ForSingletonOf<IAmazonSQS>()
                .Use(ctx => CreateAmazonConsumer(ctx.GetInstance<QueueSettings>()));

            ForSingletonOf<ISQSClient>()
                .Use<SQSClient>();

            Scan(s =>
            {
                s.AssemblyContainingType<ISQSCommand>();
                s.AddAllTypesOf<ISQSCommand>();
            });
        }

        private IAmazonSQS CreateAmazonConsumer(QueueSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.Endpoint))
                return new AmazonSQSClient();

            // localstack usage
            var config = new AmazonSQSConfig { ServiceURL = settings.Endpoint };
            return new AmazonSQSClient(config);
        }
    }
}