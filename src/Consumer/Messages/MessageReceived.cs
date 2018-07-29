
using Queueing;
using Queueing.Queueing.Models;

namespace Consumer.Messages
{
    public class MessageReceived
    {
        public ISQSCommand Message {get; set;}
    }
}