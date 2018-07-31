using Amazon.SQS;
using Amazon.SQS.Model;
using Newtonsoft.Json;
using Queueing.Configuration;
using Queueing.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using System.Net;

namespace Queueing
{
    public class SQSClient : ISQSClient
    {
        private readonly IAmazonSQS _client;
        private readonly QueueSettings _settings;
        private readonly ISQSCommand[] _messageTypes;
        private const string MessageType = "MessageType";
        private readonly ILogger _logger;

        public SQSClient(
            IAmazonSQS client,
            QueueSettings settings,
            ISQSCommand[] messageTypes,
            ILogger logger
        )
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _messageTypes = messageTypes ?? throw new ArgumentNullException(nameof(messageTypes));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task ChangeMessageVisibilityAsync(string receiptHandle, int timeoutInSeconds, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrEmpty(receiptHandle))
                throw new ArgumentNullException(nameof(receiptHandle));

            if (timeoutInSeconds <= 0)
                throw new ArgumentOutOfRangeException("Parameter cannot be lower or equal to 0.", nameof(timeoutInSeconds));

            var request = new ChangeMessageVisibilityRequest
            {
                QueueUrl = _settings.QueueUrl,
                ReceiptHandle = receiptHandle,
                VisibilityTimeout = timeoutInSeconds
            };

            return _client.ChangeMessageVisibilityAsync(request, cancellationToken);
        }

        public Task DeleteMessageAsync(string receiptHandle, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrEmpty(receiptHandle))
                throw new ArgumentNullException(nameof(receiptHandle));

            var request = new DeleteMessageRequest
            {
                QueueUrl = _settings.QueueUrl,
                ReceiptHandle = receiptHandle
            };

            return _client.DeleteMessageAsync(request, cancellationToken);
        }

        public async Task<(IList<string> Sucessful, IList<string> Failed)> DeleteMessageBatchAsync(IList<string> receiptHandles, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (receiptHandles == null)
                throw new ArgumentNullException(nameof(receiptHandles));

            var entries = receiptHandles.Select(receipt => new DeleteMessageBatchRequestEntry
            {
                Id = receipt,
                ReceiptHandle = receipt
            }).ToList();

            var response = await _client.DeleteMessageBatchAsync(_settings.QueueUrl, entries, cancellationToken);
            return (response?.Successful.Select(x => x.Id).ToList() ?? new List<string>(), response?.Failed.Select(x => x.Id).ToList() ?? receiptHandles);
        }

        public async Task<ISQSCommand> ReceiveMessageAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            var request = new ReceiveMessageRequest
            {
                QueueUrl = _settings.QueueUrl,
                MaxNumberOfMessages = 1,
                VisibilityTimeout = _settings.VisibilityTimeout,
                MessageAttributeNames = new List<string> { MessageType }
            };

            var response = await _client.ReceiveMessageAsync(request, cancellationToken);
            var message = response?.Messages?.FirstOrDefault();
            if (message == null)
                return null;

            var attribute = message.MessageAttributes.FirstOrDefault();
            var type = _messageTypes.FirstOrDefault(x => x.GetType().FullName.Equals(attribute.Value?.StringValue, StringComparison.OrdinalIgnoreCase));
            if (type == null)
                throw new NotSupportedException($"'{attribute.Value?.StringValue}' type not found.");

            var command = JsonConvert.DeserializeObject(message.Body, type.GetType()) as ISQSCommand;
            command.ReceiptHandle = message.ReceiptHandle;
            return command;
        }

        public async Task<IEnumerable<ISQSCommand>> ReceiveMessageBatchAsync(int maxNumberOfMessages = 10, CancellationToken cancellationToken = default(CancellationToken))
        {
            var request = new ReceiveMessageRequest
            {
                QueueUrl = _settings.QueueUrl,
                MaxNumberOfMessages = maxNumberOfMessages,
                VisibilityTimeout = _settings.VisibilityTimeout,
                MessageAttributeNames = new List<string> { MessageType }
            };

            var response = await _client.ReceiveMessageAsync(request, cancellationToken);
            var firstMessage = response?.Messages?.FirstOrDefault();
            if (firstMessage == null)
                return new ISQSCommand[0];

            var sqsMessages = new List<ISQSCommand>();
            foreach (var message in response.Messages)
            {
                var attribute = message.MessageAttributes.FirstOrDefault();
                var type = _messageTypes.FirstOrDefault(x => x.GetType().FullName.Equals(attribute.Value?.StringValue, StringComparison.OrdinalIgnoreCase));
                if (type == null)
                    throw new NotSupportedException($"'{attribute.Value?.StringValue}' type not found.");

                var command = JsonConvert.DeserializeObject(message.Body, type.GetType()) as ISQSCommand;
                command.ReceiptHandle = message.ReceiptHandle;
                sqsMessages.Add(command);
            }

            return sqsMessages;
        }

        public Task SendMessageBatchAsync(IList<object> messages, string queueUrl = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (messages == null)
                throw new ArgumentNullException(nameof(messages));

            var entries = messages.Select(message => new SendMessageBatchRequestEntry
                {
                    Id = Guid.NewGuid().ToString(),
                    MessageBody = JsonConvert.SerializeObject(message),
                    MessageAttributes = new Dictionary<string, MessageAttributeValue>
                    {
                        {
                            MessageType,
                            new MessageAttributeValue
                            {
                                StringValue = message.GetType().FullName,
                                DataType = "String"
                            }
                        }
                    }
                });

            queueUrl = queueUrl ?? _settings.QueueUrl;
            return _client.SendMessageBatchAsync(queueUrl, entries.ToList(), cancellationToken);
        }
    }
}