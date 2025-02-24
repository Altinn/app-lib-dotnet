// <auto-generated/>
#pragma warning disable CS0618
using Microsoft.Kiota.Abstractions.Extensions;
using Microsoft.Kiota.Abstractions.Serialization;
using System.Collections.Generic;
using System.IO;
using System;
namespace Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models
{
    [global::System.CodeDom.Compiler.GeneratedCode("Kiota", "1.0.0")]
    #pragma warning disable CS1591
    public partial class DataPatchResponseMultiple : IParsable
    #pragma warning restore CS1591
    {
        /// <summary>The instance property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.Instance? Instance { get; set; }
#nullable restore
#else
        public global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.Instance Instance { get; set; }
#endif
        /// <summary>The newDataModels property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public List<global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.DataModelPairResponse>? NewDataModels { get; set; }
#nullable restore
#else
        public List<global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.DataModelPairResponse> NewDataModels { get; set; }
#endif
        /// <summary>The validationIssues property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public List<global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.ValidationSourcePair>? ValidationIssues { get; set; }
#nullable restore
#else
        public List<global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.ValidationSourcePair> ValidationIssues { get; set; }
#endif
        /// <summary>
        /// Creates a new instance of the appropriate class based on discriminator value
        /// </summary>
        /// <returns>A <see cref="global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.DataPatchResponseMultiple"/></returns>
        /// <param name="parseNode">The parse node to use to read the discriminator value and create the object</param>
        public static global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.DataPatchResponseMultiple CreateFromDiscriminatorValue(IParseNode parseNode)
        {
            _ = parseNode ?? throw new ArgumentNullException(nameof(parseNode));
            return new global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.DataPatchResponseMultiple();
        }
        /// <summary>
        /// The deserialization information for the current model
        /// </summary>
        /// <returns>A IDictionary&lt;string, Action&lt;IParseNode&gt;&gt;</returns>
        public virtual IDictionary<string, Action<IParseNode>> GetFieldDeserializers()
        {
            return new Dictionary<string, Action<IParseNode>>
            {
                { "instance", n => { Instance = n.GetObjectValue<global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.Instance>(global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.Instance.CreateFromDiscriminatorValue); } },
                { "newDataModels", n => { NewDataModels = n.GetCollectionOfObjectValues<global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.DataModelPairResponse>(global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.DataModelPairResponse.CreateFromDiscriminatorValue)?.AsList(); } },
                { "validationIssues", n => { ValidationIssues = n.GetCollectionOfObjectValues<global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.ValidationSourcePair>(global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.ValidationSourcePair.CreateFromDiscriminatorValue)?.AsList(); } },
            };
        }
        /// <summary>
        /// Serializes information the current object
        /// </summary>
        /// <param name="writer">Serialization writer to use to serialize this model</param>
        public virtual void Serialize(ISerializationWriter writer)
        {
            _ = writer ?? throw new ArgumentNullException(nameof(writer));
            writer.WriteObjectValue<global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.Instance>("instance", Instance);
            writer.WriteCollectionOfObjectValues<global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.DataModelPairResponse>("newDataModels", NewDataModels);
            writer.WriteCollectionOfObjectValues<global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.ValidationSourcePair>("validationIssues", ValidationIssues);
        }
    }
}
#pragma warning restore CS0618
