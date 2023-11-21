using System.Net.Http.Headers;
using Altinn.App.Core.Features.Payment.Providers.Nets.Models;
using Microsoft.Extensions.Options;

namespace Altinn.App.Core.Features.Payment.Providers.Nets;


public class NetsClient
{
    private readonly HttpClient _httpClient;
    private readonly NetsPaymentSettings _settings;

    public NetsClient(HttpClient httpClient, IOptions<NetsPaymentSettings> settings)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(_settings.SecretApiKey);
        // _httpClient.DefaultRequestHeaders.Add("CommercePlatformTag", "Altinn 3");
    }
    
    /// <summary>
    /// Initializes a new payment object that becomes the object used throughout the checkout flow for a particular customer and order. Creating a payment object is the first step when you intend to accept a payment from your customer. Entering the amount 100 corresponds to 1 unit of the currency entered, such as e.g. 1 NOK. Typically you provide the following information:
    /// The order details including order items, total amount, and currency.
    /// Checkout page settings, which specify what type of integration you want: a checkout page embedded on your site or a pre-built checkout page hosted by Nexi Group. You can also specify data about your customer so that your customer only needs to provide payment details on the checkout page.
    /// 
    /// Optionally, you can also provide information regarding:
    /// Notifications if you want to be notified through webhooks when the status of the payment changes.
    /// Fees added when using payment methods such as invoice.
    /// Charge set to true so you can enable autocapture for subscriptions.
    /// 
    /// On success, this method returns a paymentId that can be used in subsequent requests to refer to the newly created payment object. Optionally, the response object will also contain a hostedPaymentPageUrl, which is the URL you should redirect to if using a hosted pre-built checkout page.
    /// </summary>
    public async Task<HttpApiResult<NetsPaymentSuccess>> CreatePayment(NetsCreatePayment payment)
    {
        var response = await _httpClient.PostAsJsonAsync("/v1/payments", payment);
        return await HttpApiResult<NetsPaymentSuccess>.FromHttpResponse(response);
    }

    public async Task<HttpApiResult<NetsPaymentFull>> RetrievePayment(string paymentId)
    {
        var response = await _httpClient.GetAsync($"/v1/payments/{paymentId}");
        return await HttpApiResult<NetsPaymentFull>.FromHttpResponse(response);
    }
}