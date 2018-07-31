using Amazon.SQS.Model;
using Queueing.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Queueing
{
    public interface ISQSClient
    {
        Task<ISQSCommand> ReceiveMessageAsync(CancellationToken cancellationToken = default(CancellationToken));
        Task<IEnumerable<ISQSCommand>> ReceiveMessageBatchAsync(int maxNumberOfMessages = 10, CancellationToken cancellationToken = default(CancellationToken));
        Task SendMessageBatchAsync(IList<object> messages, string queueUrl = null, CancellationToken cancellationToken = default(CancellationToken));
        Task DeleteMessageAsync(string receiptHandle, CancellationToken cancellationToken = default(CancellationToken));
        Task<(IList<string> Sucessful, IList<string> Failed)> DeleteMessageBatchAsync(IList<string> receiptHandles, CancellationToken cancellationToken = default(CancellationToken));
        Task ChangeMessageVisibilityAsync(string receiptHandle, int timeoutInSeconds, CancellationToken cancellationToken = default(CancellationToken));
    }
}