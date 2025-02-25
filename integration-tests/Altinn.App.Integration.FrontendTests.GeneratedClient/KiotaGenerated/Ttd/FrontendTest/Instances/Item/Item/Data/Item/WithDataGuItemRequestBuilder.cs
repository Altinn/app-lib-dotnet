// <auto-generated/>
#pragma warning disable CS0618
using Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Ttd.FrontendTest.Instances.Item.Item.Data.Item.TypeNamespace;
using Microsoft.Kiota.Abstractions.Extensions;
using Microsoft.Kiota.Abstractions.Serialization;
using Microsoft.Kiota.Abstractions;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System;
namespace Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Ttd.FrontendTest.Instances.Item.Item.Data.Item
{
    /// <summary>
    /// Delete data element
    /// </summary>
    [global::System.CodeDom.Compiler.GeneratedCode("Kiota", "1.0.0")]
    public partial class WithDataGuItemRequestBuilder : BaseRequestBuilder
    {
        /// <summary>The type property</summary>
        public global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Ttd.FrontendTest.Instances.Item.Item.Data.Item.TypeNamespace.TypeRequestBuilder Type
        {
            get => new global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Ttd.FrontendTest.Instances.Item.Item.Data.Item.TypeNamespace.TypeRequestBuilder(PathParameters, RequestAdapter);
        }
        /// <summary>
        /// Instantiates a new <see cref="global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Ttd.FrontendTest.Instances.Item.Item.Data.Item.WithDataGuItemRequestBuilder"/> and sets the default values.
        /// </summary>
        /// <param name="pathParameters">Path parameters for the request</param>
        /// <param name="requestAdapter">The request adapter to use to execute the requests.</param>
        public WithDataGuItemRequestBuilder(Dictionary<string, object> pathParameters, IRequestAdapter requestAdapter) : base(requestAdapter, "{+baseurl}/ttd/frontend-test/instances/{instanceOwnerPartyId}/{instanceGuid}/data/{dataGuid}{?language*}", pathParameters)
        {
        }
        /// <summary>
        /// Instantiates a new <see cref="global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Ttd.FrontendTest.Instances.Item.Item.Data.Item.WithDataGuItemRequestBuilder"/> and sets the default values.
        /// </summary>
        /// <param name="rawUrl">The raw URL to use for the request builder.</param>
        /// <param name="requestAdapter">The request adapter to use to execute the requests.</param>
        public WithDataGuItemRequestBuilder(string rawUrl, IRequestAdapter requestAdapter) : base(requestAdapter, "{+baseurl}/ttd/frontend-test/instances/{instanceOwnerPartyId}/{instanceGuid}/data/{dataGuid}{?language*}", rawUrl)
        {
        }
        /// <summary>
        /// Delete data for a specific data element
        /// </summary>
        /// <param name="cancellationToken">Cancellation token to use when cancelling requests</param>
        /// <param name="requestConfiguration">Configuration for the request such as headers, query parameters, and middleware options.</param>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public async Task DeleteAsync(Action<RequestConfiguration<global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Ttd.FrontendTest.Instances.Item.Item.Data.Item.WithDataGuItemRequestBuilder.WithDataGuItemRequestBuilderDeleteQueryParameters>>? requestConfiguration = default, CancellationToken cancellationToken = default)
        {
#nullable restore
#else
        public async Task DeleteAsync(Action<RequestConfiguration<global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Ttd.FrontendTest.Instances.Item.Item.Data.Item.WithDataGuItemRequestBuilder.WithDataGuItemRequestBuilderDeleteQueryParameters>> requestConfiguration = default, CancellationToken cancellationToken = default)
        {
#endif
            var requestInfo = ToDeleteRequestInformation(requestConfiguration);
            await RequestAdapter.SendNoContentAsync(requestInfo, default, cancellationToken).ConfigureAwait(false);
        }
        /// <summary>
        /// Delete data for a specific data element
        /// </summary>
        /// <returns>A <see cref="RequestInformation"/></returns>
        /// <param name="requestConfiguration">Configuration for the request such as headers, query parameters, and middleware options.</param>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public RequestInformation ToDeleteRequestInformation(Action<RequestConfiguration<global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Ttd.FrontendTest.Instances.Item.Item.Data.Item.WithDataGuItemRequestBuilder.WithDataGuItemRequestBuilderDeleteQueryParameters>>? requestConfiguration = default)
        {
#nullable restore
#else
        public RequestInformation ToDeleteRequestInformation(Action<RequestConfiguration<global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Ttd.FrontendTest.Instances.Item.Item.Data.Item.WithDataGuItemRequestBuilder.WithDataGuItemRequestBuilderDeleteQueryParameters>> requestConfiguration = default)
        {
#endif
            var requestInfo = new RequestInformation(Method.DELETE, UrlTemplate, PathParameters);
            requestInfo.Configure(requestConfiguration);
            return requestInfo;
        }
        /// <summary>
        /// Returns a request builder with the provided arbitrary URL. Using this method means any other path or query parameters are ignored.
        /// </summary>
        /// <returns>A <see cref="global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Ttd.FrontendTest.Instances.Item.Item.Data.Item.WithDataGuItemRequestBuilder"/></returns>
        /// <param name="rawUrl">The raw URL to use for the request builder.</param>
        public global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Ttd.FrontendTest.Instances.Item.Item.Data.Item.WithDataGuItemRequestBuilder WithUrl(string rawUrl)
        {
            return new global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Ttd.FrontendTest.Instances.Item.Item.Data.Item.WithDataGuItemRequestBuilder(rawUrl, RequestAdapter);
        }
        /// <summary>
        /// Delete data for a specific data element
        /// </summary>
        [global::System.CodeDom.Compiler.GeneratedCode("Kiota", "1.0.0")]
        public partial class WithDataGuItemRequestBuilderDeleteQueryParameters 
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
