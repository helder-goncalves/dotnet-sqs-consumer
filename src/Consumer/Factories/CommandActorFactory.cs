using System;
using Consumer.Actors;
using Queueing.Models;
using Proto;

namespace Consumer.Factories
{
    public class CommandActorFactory : ICommandActorFactory
    {
        private readonly IActorFactory _factory;

        public CommandActorFactory(IActorFactory factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public PID GetHandler(ISQSCommand command)
        {
            switch (command)
            {
                case WebhookCommand webhook:
                    return _factory.GetActor<Dispatcher>($"Dispatcher_{webhook.CommandId}");
                default:
                    throw new NotSupportedException($"{command.GetType()} is not supported");
            }
        }
    }
}