using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Proto;
using Serilog;
using Serilog.Context;
using Consumer.Messages;
using Queueing;
using SerilogTimings.Extensions;
using Queueing.Models;
using Consumer.Factories;

namespace Consumer.Actors
{
    public class Dequeuer : IActor
    {
        private readonly ISQSClient _sqsClient;
        private readonly ILogger _logger;
        private readonly ICommandActorFactory _factory;
        private readonly IMessageMapper _mapper;

        public Dequeuer(
            ISQSClient sqsClient,
            ILogger logger,
            ICommandActorFactory factory,
            IMessageMapper mapper)
        {
            _sqsClient = sqsClient ?? throw new ArgumentNullException(nameof(sqsClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public async Task ReceiveAsync(IContext context)
        {
            switch (context.Message)
            {
                case Started _:
                {
                    context.Self.Tell(new ReceiveMessages());
                }
                break;

                case ReceiveMessages receive:
                {
                    var commands = await _sqsClient.ReceiveMessageBatchAsync(receive.NumberOfMessages);
                    if (commands.Count() > 0)
                    {
                        foreach (var command in commands)
                        {
                            var handler = _factory.GetHandler(command);
                            var message = _mapper.Map(command);
                            handler.Tell(message);
                        }

                        context.Self.Tell(new ReceiveMessages());
                    }
                    else
                    {
                        context.Self.Tell(new BackOff());
                    }
                }
                break;

                case BackOff backOff:
                {
                    await Task.Delay(TimeSpan.FromSeconds(backOff.DelayInSeconds));
                    context.Self.Tell(new ReceiveMessages());
                }
                break;
            }
        }
    }
}