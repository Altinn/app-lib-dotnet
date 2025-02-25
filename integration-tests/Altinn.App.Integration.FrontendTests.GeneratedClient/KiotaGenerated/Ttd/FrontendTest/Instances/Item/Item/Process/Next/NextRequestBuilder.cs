// <auto-generated/>
#pragma warning disable CS0618
using Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models;
using Microsoft.Kiota.Abstractions.Extensions;
using Microsoft.Kiota.Abstractions.Serialization;
using Microsoft.Kiota.Abstractions;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System;
namespace Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Ttd.FrontendTest.Instances.Item.Item.Process.Next
{
    /// <summary>
    /// Move instance to next process step
    /// </summary>
    [global::System.CodeDom.Compiler.GeneratedCode("Kiota", "1.0.0")]
    public partial class NextRequestBuilder : BaseRequestBuilder
    {
        /// <summary>
        /// Instantiates a new <see cref="global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Ttd.FrontendTest.Instances.Item.Item.Process.Next.NextRequestBuilder"/> and sets the default values.
        /// </summary>
        /// <param name="pathParameters">Path parameters for the request</param>
        /// <param name="requestAdapter">The request adapter to use to execute the requests.</param>
        public NextRequestBuilder(Dictionary<string, object> pathParameters, IRequestAdapter requestAdapter) : base(requestAdapter, "{+baseurl}/ttd/frontend-test/instances/{instanceOwnerPartyId}/{instanceGuid}/process/next{?language*}", pathParameters)
        {
        }
        /// <summary>
        /// Instantiates a new <see cref="global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Ttd.FrontendTest.Instances.Item.Item.Process.Next.NextRequestBuilder"/> and sets the default values.
        /// </summary>
        /// <param name="rawUrl">The raw URL to use for the request builder.</param>
        /// <param name="requestAdapter">The request adapter to use to execute the requests.</param>
        public NextRequestBuilder(string rawUrl, IRequestAdapter requestAdapter) : base(requestAdapter, "{+baseurl}/ttd/frontend-test/instances/{instanceOwnerPartyId}/{instanceGuid}/process/next{?language*}", rawUrl)
        {
        }
        /// <summary>
        /// Move the instance to the next process step
        /// </summary>
        /// <returns>A <see cref="global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.AppProcessState"/></returns>
        /// <param name="body">The request body</param>
        /// <param name="cancellationToken">Cancellation token to use when cancelling requests</param>
        /// <param name="requestConfiguration">Configuration for the request such as headers, query parameters, and middleware options.</param>
        /// <exception cref="global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.AppProcessState400Error">When receiving a 400 status code</exception>
        /// <exception cref="global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.AppProcessState401Error">When receiving a 401 status code</exception>
        /// <exception cref="global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.AppProcessState403Error">When receiving a 403 status code</exception>
        /// <exception cref="global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.AppProcessState404Error">When receiving a 404 status code</exception>
        /// <exception cref="global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.AppProcessState409Error">When receiving a 409 status code</exception>
        /// <exception cref="global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.AppProcessState500Error">When receiving a 500 status code</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public async Task<global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.AppProcessState?> PutAsync(global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.ProcessNext body, Action<RequestConfiguration<global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Ttd.FrontendTest.Instances.Item.Item.Process.Next.NextRequestBuilder.NextRequestBuilderPutQueryParameters>>? requestConfiguration = default, CancellationToken cancellationToken = default)
        {
#nullable restore
#else
        public async Task<global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.AppProcessState> PutAsync(global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.ProcessNext body, Action<RequestConfiguration<global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Ttd.FrontendTest.Instances.Item.Item.Process.Next.NextRequestBuilder.NextRequestBuilderPutQueryParameters>> requestConfiguration = default, CancellationToken cancellationToken = default)
        {
#endif
            _ = body ?? throw new ArgumentNullException(nameof(body));
            var requestInfo = ToPutRequestInformation(body, requestConfiguration);
            var errorMapping = new Dictionary<string, ParsableFactory<IParsable>>
            {
                { "400", global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.AppProcessState400Error.CreateFromDiscriminatorValue },
                { "401", global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.AppProcessState401Error.CreateFromDiscriminatorValue },
                { "403", global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.AppProcessState403Error.CreateFromDiscriminatorValue },
                { "404", global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.AppProcessState404Error.CreateFromDiscriminatorValue },
                { "409", global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.AppProcessState409Error.CreateFromDiscriminatorValue },
                { "500", global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.AppProcessState500Error.CreateFromDiscriminatorValue },
            };
            return await RequestAdapter.SendAsync<global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.AppProcessState>(requestInfo, global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.AppProcessState.CreateFromDiscriminatorValue, errorMapping, cancellationToken).ConfigureAwait(false);
        }
        /// <summary>
        /// Move the instance to the next process step
        /// </summary>
        /// <returns>A <see cref="RequestInformation"/></returns>
        /// <param name="body">The request body</param>
        /// <param name="requestConfiguration">Configuration for the request such as headers, query parameters, and middleware options.</param>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public RequestInformation ToPutRequestInformation(global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.ProcessNext body, Action<RequestConfiguration<global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Ttd.FrontendTest.Instances.Item.Item.Process.Next.NextRequestBuilder.NextRequestBuilderPutQueryParameters>>? requestConfiguration = default)
        {
#nullable restore
#else
        public RequestInformation ToPutRequestInformation(global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.ProcessNext body, Action<RequestConfiguration<global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Ttd.FrontendTest.Instances.Item.Item.Process.Next.NextRequestBuilder.NextRequestBuilderPutQueryParameters>> requestConfiguration = default)
        {
#endif
            _ = body ?? throw new ArgumentNullException(nameof(body));
            var requestInfo = new RequestInformation(Method.PUT, UrlTemplate, PathParameters);
            requestInfo.Configure(requestConfiguration);
            requestInfo.Headers.TryAdd("Accept", "application/json");
            requestInfo.SetContentFromParsable(RequestAdapter, "application/json", body);
            return requestInfo;
        }
        /// <summary>
        /// Returns a request builder with the provided arbitrary URL. Using this method means any other path or query parameters are ignored.
        /// </summary>
        /// <returns>A <see cref="global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Ttd.FrontendTest.Instances.Item.Item.Process.Next.NextRequestBuilder"/></returns>
        /// <param name="rawUrl">The raw URL to use for the request builder.</param>
        public global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Ttd.FrontendTest.Instances.Item.Item.Process.Next.NextRequestBuilder WithUrl(string rawUrl)
        {
            return new global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Ttd.FrontendTest.Instances.Item.Item.Process.Next.NextRequestBuilder(rawUrl, RequestAdapter);
        }
        /// <summary>
        /// Move the instance to the next process step
        /// </summary>
        [global::System.CodeDom.Compiler.GeneratedCode("Kiota", "1.0.0")]
        public partial class NextRequestBuilderPutQueryParameters 
        {
            /// <summary>Some apps make changes to the data models or validation based on the active language of the user</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
            [QueryParameter("language")]
            public string? Language { get; set; }
#nullable restore
#else
            [QueryParameter("language")]
            public string Language { get; set; }
#endif
        }
    }
}
#pragma warning restore CS0618
