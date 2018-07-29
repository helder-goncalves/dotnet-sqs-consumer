using Queueing.Queueing.Models;

namespace Consumer.Factories
{
    public interface IMessageMapper
    {
        object Map(ISQSCommand command);
    }
}