using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Proto;
using Serilog;
using SerilogTimings.Extensions;
using Consumer.Messages;

namespace Consumer.Actors
{
    public class Dispatcher : IActor
    {
        private readonly ILogger _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IActorFactory _actorFactory;

        public Dispatcher(ILogger logger, IHttpClientFactory httpClientFactory, IActorFactory actorFactory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _actorFactory = actorFactory ?? throw new ArgumentNullException(nameof(actorFactory));
        }

        public async Task ReceiveAsync(IContext context)
        {
            switch (context.Message)
            {
                case Started _:
                {
                    context.SetReceiveTimeout(TimeSpan.FromSeconds(5));
                }
                break;

                case SendRequest sendRequest:
                {
                    using (_logger.TimeOperation("{ActorId} sending request to {Url}", context.Self.Id, sendRequest.Url))
                    using (var client = _httpClientFactory.CreateClient(nameof(Dispatcher)))
                    {
                        var content = new StringContent(GetJsonData(), Encoding.UTF8, "application/json");
                        await client.PostAsync(sendRequest.Url, content);
                    }

                    var deleter = _actorFactory.GetActor<Deleter>();

                    deleter.Tell(
                        new DeleteMessage
                        {
                            MessageId = sendRequest.MessageId,
                            ReceiptHandle = sendRequest.ReceiptHandle
                        });
                }
                break;

                case ReceiveTimeout _:
                {
                    context.Self.Stop();
                }
                break;
            }
        }

        private string GetJsonData()
        {
            return "{\"data\":{" +
                    "\"email\":\"test352@checkout.com\"," +
                    "\"transactionIndicator\":0," +
                    "\"customerIp\":\"127.0.0.1\"," +
                    "\"authCode\":\"00000\"," +
                    "\"isCascaded\":false," +
                    "\"autoCapture\":\"N\"," +
                    "\"autoCapTime\":0," +
                    "\"card\":{" +
                    "\"customerId\":\"afa89918-7083-47ad-b17e-bd3e27c953ea\"," +
                    "\"expiryMonth\":\"06\"," +
                    "\"expiryYear\":\"2018\"," +
                    "\"billingDetails\":{\"phone\":{}}," +
                    "\"id\":\"bd8c59b1-6e70-447c-9f42-6474e2bf289c\"," +
                    "\"last4\":\"424242******4242\"," +
                    "\"paymentMethod\":\"VISA\"," +
                    "\"fingerprint\":\"f639cab2745bee4140bf86df6b6d6e255c5945aac3788d923fa047ea4c208622\"," +
                    "\"name\":\"Testf7c49127-2d13-4bba-af78-1b0045145553\"," +
                    "\"cvvCheck\":\"Y\"," +
                    "\"avsCheck\":\"S\"}," +
                    "\"riskCheck\":false," +
                    "\"customerPaymentPlans\":[]," +
                    "\"shippingDetails\":{" +
                    "\"addressLine1\":\"333CormierBypass\"," +
                    "\"addressLine2\":\"RolfsonAlley\"," +
                    "\"postCode\":\"ue02ou\"," +
                    "\"country\":\"US\"," +
                    "\"city\":\"Schmittchester\"," +
                    "\"state\":\"Jakubowskiton\"," +
                    "\"phone\":{\"countryCode\":\"77\",\"number\":\"456456456\"}}," +
                    "\"binData\":{\"bin\":\"424242\",\"countryName\":\"CountryName\",\"fundingSource\":\"FundingSource\"}," +
                    "\"eventId\":\"6aaaefdc-4638-4f6f-80db-7faa902d9865\"," +
                    "\"accountId\":100002," +
                    "\"businessId\":100003," +
                    "\"channelId\":100005," +
                    "\"id\":\"charge_test_3EB5FCCAFE777V5472AD\"," +
                    "\"liveMode\":false," +
                    "\"chargeMode\":1," +
                    "\"responseCode\":\"10000\"," +
                    "\"created\":\"2017-06-21T13:43:46.2830772Z\"," +
                    "\"value\":200," +
                    "\"currency\":\"USD\"," +
                    "\"trackId\":\"Tfdgjk8c8fb5b4-5a67-4f00-af27-f0064bcf01cc\"," +
                    "\"description\":\"chargedescription\"," +
                    "\"responseMessage\":\"Failed\"," +
                    "\"responseAdvancedInfo\":\"Failed\"," +
                    "\"status\":\"Declined\"," +
                    "\"metadata\":{\"key1\":\"value1\"}," +
                    "\"products\":[{\"name\":\"Tablet1goldlimited\",\"description\":\"Tablet1goldlimited\",\"sku\":\"1aab2aa\",\"price\":100,\"quantity\":1,\"shippingCost\":10,\"trackingUrl\":\"https://www.tracker.com\"}]}," +
                    "\"notifications\":[]}";
        }
    }
}