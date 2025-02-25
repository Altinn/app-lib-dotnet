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
namespace Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Ttd.FrontendTest.Instances.Item.Item.Data.Item.TypeNamespace.Datalist
{
    /// <summary>
    /// CRUD operations for data of type datalist
    /// </summary>
    [global::System.CodeDom.Compiler.GeneratedCode("Kiota", "1.0.0")]
    public partial class DatalistRequestBuilder : BaseRequestBuilder
    {
        /// <summary>
        /// Instantiates a new <see cref="global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Ttd.FrontendTest.Instances.Item.Item.Data.Item.TypeNamespace.Datalist.DatalistRequestBuilder"/> and sets the default values.
        /// </summary>
        /// <param name="pathParameters">Path parameters for the request</param>
        /// <param name="requestAdapter">The request adapter to use to execute the requests.</param>
        public DatalistRequestBuilder(Dictionary<string, object> pathParameters, IRequestAdapter requestAdapter) : base(requestAdapter, "{+baseurl}/ttd/frontend-test/instances/{instanceOwnerPartyId}/{instanceGuid}/data/{dataGuid}/type/datalist{?language*}", pathParameters)
        {
        }
        /// <summary>
        /// Instantiates a new <see cref="global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Ttd.FrontendTest.Instances.Item.Item.Data.Item.TypeNamespace.Datalist.DatalistRequestBuilder"/> and sets the default values.
        /// </summary>
        /// <param name="rawUrl">The raw URL to use for the request builder.</param>
        /// <param name="requestAdapter">The request adapter to use to execute the requests.</param>
        public DatalistRequestBuilder(string rawUrl, IRequestAdapter requestAdapter) : base(requestAdapter, "{+baseurl}/ttd/frontend-test/instances/{instanceOwnerPartyId}/{instanceGuid}/data/{dataGuid}/type/datalist{?language*}", rawUrl)
        {
        }
        /// <summary>
        /// Get data for a specific data elementsee [JSON Schema](/ttd/frontend-test/api/jsonschema/datalist)
        /// </summary>
        /// <returns>A <see cref="global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.Datalist"/></returns>
        /// <param name="cancellationToken">Cancellation token to use when cancelling requests</param>
        /// <param name="requestConfiguration">Configuration for the request such as headers, query parameters, and middleware options.</param>
        /// <exception cref="global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.Datalist400Error">When receiving a 400 status code</exception>
        /// <exception cref="global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.Datalist401Error">When receiving a 401 status code</exception>
        /// <exception cref="global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.Datalist403Error">When receiving a 403 status code</exception>
        /// <exception cref="global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.Datalist404Error">When receiving a 404 status code</exception>
        /// <exception cref="global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.Datalist500Error">When receiving a 500 status code</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public async Task<global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.Datalist?> GetAsync(Action<RequestConfiguration<global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Ttd.FrontendTest.Instances.Item.Item.Data.Item.TypeNamespace.Datalist.DatalistRequestBuilder.DatalistRequestBuilderGetQueryParameters>>? requestConfiguration = default, CancellationToken cancellationToken = default)
        {
#nullable restore
#else
        public async Task<global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.Datalist> GetAsync(Action<RequestConfiguration<global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Ttd.FrontendTest.Instances.Item.Item.Data.Item.TypeNamespace.Datalist.DatalistRequestBuilder.DatalistRequestBuilderGetQueryParameters>> requestConfiguration = default, CancellationToken cancellationToken = default)
        {
#endif
            var requestInfo = ToGetRequestInformation(requestConfiguration);
            var errorMapping = new Dictionary<string, ParsableFactory<IParsable>>
            {
                { "400", global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.Datalist400Error.CreateFromDiscriminatorValue },
                { "401", global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.Datalist401Error.CreateFromDiscriminatorValue },
                { "403", global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.Datalist403Error.CreateFromDiscriminatorValue },
                { "404", global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.Datalist404Error.CreateFromDiscriminatorValue },
                { "500", global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.Datalist500Error.CreateFromDiscriminatorValue },
            };
            return await RequestAdapter.SendAsync<global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.Datalist>(requestInfo, global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.Datalist.CreateFromDiscriminatorValue, errorMapping, cancellationToken).ConfigureAwait(false);
        }
        /// <summary>
        /// Update data for a specific data element
        /// </summary>
        /// <returns>A <see cref="Stream"/></returns>
        /// <param name="body">The request body</param>
        /// <param name="cancellationToken">Cancellation token to use when cancelling requests</param>
        /// <param name="requestConfiguration">Configuration for the request such as headers, query parameters, and middleware options.</param>
        /// <exception cref="global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.Datalist400Error">When receiving a 400 status code</exception>
        /// <exception cref="global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.Datalist401Error">When receiving a 401 status code</exception>
        /// <exception cref="global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.Datalist403Error">When receiving a 403 status code</exception>
        /// <exception cref="global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.Datalist404Error">When receiving a 404 status code</exception>
        /// <exception cref="global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.Datalist500Error">When receiving a 500 status code</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public async Task<Stream?> PutAsync(global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.Datalist body, Action<RequestConfiguration<global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Ttd.FrontendTest.Instances.Item.Item.Data.Item.TypeNamespace.Datalist.DatalistRequestBuilder.DatalistRequestBuilderPutQueryParameters>>? requestConfiguration = default, CancellationToken cancellationToken = default)
        {
#nullable restore
#else
        public async Task<Stream> PutAsync(global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.Datalist body, Action<RequestConfiguration<global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Ttd.FrontendTest.Instances.Item.Item.Data.Item.TypeNamespace.Datalist.DatalistRequestBuilder.DatalistRequestBuilderPutQueryParameters>> requestConfiguration = default, CancellationToken cancellationToken = default)
        {
#endif
            _ = body ?? throw new ArgumentNullException(nameof(body));
            var requestInfo = ToPutRequestInformation(body, requestConfiguration);
            var errorMapping = new Dictionary<string, ParsableFactory<IParsable>>
            {
                { "400", global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.Datalist400Error.CreateFromDiscriminatorValue },
                { "401", global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.Datalist401Error.CreateFromDiscriminatorValue },
                { "403", global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.Datalist403Error.CreateFromDiscriminatorValue },
                { "404", global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.Datalist404Error.CreateFromDiscriminatorValue },
                { "500", global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.Datalist500Error.CreateFromDiscriminatorValue },
            };
            return await RequestAdapter.SendPrimitiveAsync<Stream>(requestInfo, errorMapping, cancellationToken).ConfigureAwait(false);
        }
        /// <summary>
        /// Get data for a specific data elementsee [JSON Schema](/ttd/frontend-test/api/jsonschema/datalist)
        /// </summary>
        /// <returns>A <see cref="RequestInformation"/></returns>
        /// <param name="requestConfiguration">Configuration for the request such as headers, query parameters, and middleware options.</param>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public RequestInformation ToGetRequestInformation(Action<RequestConfiguration<global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Ttd.FrontendTest.Instances.Item.Item.Data.Item.TypeNamespace.Datalist.DatalistRequestBuilder.DatalistRequestBuilderGetQueryParameters>>? requestConfiguration = default)
        {
#nullable restore
#else
        public RequestInformation ToGetRequestInformation(Action<RequestConfiguration<global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Ttd.FrontendTest.Instances.Item.Item.Data.Item.TypeNamespace.Datalist.DatalistRequestBuilder.DatalistRequestBuilderGetQueryParameters>> requestConfiguration = default)
        {
#endif
            var requestInfo = new RequestInformation(Method.GET, UrlTemplate, PathParameters);
            requestInfo.Configure(requestConfiguration);
            requestInfo.Headers.TryAdd("Accept", "application/json");
            return requestInfo;
        }
        /// <summary>
        /// Update data for a specific data element
        /// </summary>
        /// <returns>A <see cref="RequestInformation"/></returns>
        /// <param name="body">The request body</param>
        /// <param name="requestConfiguration">Configuration for the request such as headers, query parameters, and middleware options.</param>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public RequestInformation ToPutRequestInformation(global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.Datalist body, Action<RequestConfiguration<global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Ttd.FrontendTest.Instances.Item.Item.Data.Item.TypeNamespace.Datalist.DatalistRequestBuilder.DatalistRequestBuilderPutQueryParameters>>? requestConfiguration = default)
        {
#nullable restore
#else
        public RequestInformation ToPutRequestInformation(global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.Datalist body, Action<RequestConfiguration<global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Ttd.FrontendTest.Instances.Item.Item.Data.Item.TypeNamespace.Datalist.DatalistRequestBuilder.DatalistRequestBuilderPutQueryParameters>> requestConfiguration = default)
        {
#endif
            _ = body ?? throw new ArgumentNullException(nameof(body));
            var requestInfo = new RequestInformation(Method.PUT, UrlTemplate, PathParameters);
            requestInfo.Configure(requestConfiguration);
            requestInfo.Headers.TryAdd("Accept", "application/problem+json");
            requestInfo.SetContentFromParsable(RequestAdapter, "application/json", body);
            return requestInfo;
        }
        /// <summary>
        /// Returns a request builder with the provided arbitrary URL. Using this method means any other path or query parameters are ignored.
        /// </summary>
        /// <returns>A <see cref="global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Ttd.FrontendTest.Instances.Item.Item.Data.Item.TypeNamespace.Datalist.DatalistRequestBuilder"/></returns>
        /// <param name="rawUrl">The raw URL to use for the request builder.</param>
        public global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Ttd.FrontendTest.Instances.Item.Item.Data.Item.TypeNamespace.Datalist.DatalistRequestBuilder WithUrl(string rawUrl)
        {
            return new global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Ttd.FrontendTest.Instances.Item.Item.Data.Item.TypeNamespace.Datalist.DatalistRequestBuilder(rawUrl, RequestAdapter);
        }
        /// <summary>
        /// Get data for a specific data elementsee [JSON Schema](/ttd/frontend-test/api/jsonschema/datalist)
        /// </summary>
        [global::System.CodeDom.Compiler.GeneratedCode("Kiota", "1.0.0")]
        public partial class DatalistRequestBuilderGetQueryParameters 
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
        /// <summary>
        /// Update data for a specific data element
        /// </summary>
        [global::System.CodeDom.Compiler.GeneratedCode("Kiota", "1.0.0")]
        public partial class DatalistRequestBuilderPutQueryParameters 
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
