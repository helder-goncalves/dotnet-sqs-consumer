using Queueing.Queueing.Models;
using Proto;

namespace Consumer.Factories
{
    public interface ICommandActorFactory
    {
        PID GetHandler(ISQSCommand command);
    }
}