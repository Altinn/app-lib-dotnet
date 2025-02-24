// <auto-generated/>
#pragma warning disable CS0618
using Microsoft.Kiota.Abstractions.Extensions;
using Microsoft.Kiota.Abstractions.Serialization;
using Microsoft.Kiota.Abstractions;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System;
namespace Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Ttd.FrontendTest.Instances.Item.Item.Data.TypeNamespace.SubUploader
{
    /// <summary>
    /// CRUD operations for data of type subUploader
    /// </summary>
    [global::System.CodeDom.Compiler.GeneratedCode("Kiota", "1.0.0")]
    public partial class SubUploaderRequestBuilder : BaseRequestBuilder
    {
        /// <summary>
        /// Instantiates a new <see cref="global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Ttd.FrontendTest.Instances.Item.Item.Data.TypeNamespace.SubUploader.SubUploaderRequestBuilder"/> and sets the default values.
        /// </summary>
        /// <param name="pathParameters">Path parameters for the request</param>
        /// <param name="requestAdapter">The request adapter to use to execute the requests.</param>
        public SubUploaderRequestBuilder(Dictionary<string, object> pathParameters, IRequestAdapter requestAdapter) : base(requestAdapter, "{+baseurl}/ttd/frontend-test/instances/{instanceOwnerPartyId}/{instanceGuid}/data/type/subUploader", pathParameters)
        {
        }
        /// <summary>
        /// Instantiates a new <see cref="global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Ttd.FrontendTest.Instances.Item.Item.Data.TypeNamespace.SubUploader.SubUploaderRequestBuilder"/> and sets the default values.
        /// </summary>
        /// <param name="rawUrl">The raw URL to use for the request builder.</param>
        /// <param name="requestAdapter">The request adapter to use to execute the requests.</param>
        public SubUploaderRequestBuilder(string rawUrl, IRequestAdapter requestAdapter) : base(requestAdapter, "{+baseurl}/ttd/frontend-test/instances/{instanceOwnerPartyId}/{instanceGuid}/data/type/subUploader", rawUrl)
        {
        }
        /// <summary>
        /// Create attachment
        /// </summary>
        /// <param name="body">Binary request body</param>
        /// <param name="cancellationToken">Cancellation token to use when cancelling requests</param>
        /// <param name="requestConfiguration">Configuration for the request such as headers, query parameters, and middleware options.</param>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public async Task PostAsync(Stream body, Action<RequestConfiguration<DefaultQueryParameters>>? requestConfiguration = default, CancellationToken cancellationToken = default)
        {
#nullable restore
#else
        public async Task PostAsync(Stream body, Action<RequestConfiguration<DefaultQueryParameters>> requestConfiguration = default, CancellationToken cancellationToken = default)
        {
#endif
            _ = body ?? throw new ArgumentNullException(nameof(body));
            var requestInfo = ToPostRequestInformation(body, requestConfiguration);
            await RequestAdapter.SendNoContentAsync(requestInfo, default, cancellationToken).ConfigureAwait(false);
        }
        /// <summary>
        /// Create attachment
        /// </summary>
        /// <returns>A <see cref="RequestInformation"/></returns>
        /// <param name="body">Binary request body</param>
        /// <param name="requestConfiguration">Configuration for the request such as headers, query parameters, and middleware options.</param>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public RequestInformation ToPostRequestInformation(Stream body, Action<RequestConfiguration<DefaultQueryParameters>>? requestConfiguration = default)
        {
#nullable restore
#else
        public RequestInformation ToPostRequestInformation(Stream body, Action<RequestConfiguration<DefaultQueryParameters>> requestConfiguration = default)
        {
#endif
            _ = body ?? throw new ArgumentNullException(nameof(body));
            var requestInfo = new RequestInformation(Method.POST, UrlTemplate, PathParameters);
            requestInfo.Configure(requestConfiguration);
            requestInfo.SetStreamContent(body, "application/octet-stream");
            return requestInfo;
        }
        /// <summary>
        /// Returns a request builder with the provided arbitrary URL. Using this method means any other path or query parameters are ignored.
        /// </summary>
        /// <returns>A <see cref="global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Ttd.FrontendTest.Instances.Item.Item.Data.TypeNamespace.SubUploader.SubUploaderRequestBuilder"/></returns>
        /// <param name="rawUrl">The raw URL to use for the request builder.</param>
        public global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Ttd.FrontendTest.Instances.Item.Item.Data.TypeNamespace.SubUploader.SubUploaderRequestBuilder WithUrl(string rawUrl)
        {
            return new global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Ttd.FrontendTest.Instances.Item.Item.Data.TypeNamespace.SubUploader.SubUploaderRequestBuilder(rawUrl, RequestAdapter);
        }
    }
}
#pragma warning restore CS0618
