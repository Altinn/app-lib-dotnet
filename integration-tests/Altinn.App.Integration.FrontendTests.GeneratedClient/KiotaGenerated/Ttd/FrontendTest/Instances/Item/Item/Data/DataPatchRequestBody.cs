// <auto-generated/>
#pragma warning disable CS0618
using Microsoft.Kiota.Abstractions.Extensions;
using Microsoft.Kiota.Abstractions.Serialization;
using System.Collections.Generic;
using System.IO;
using System;
namespace Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Ttd.FrontendTest.Instances.Item.Item.Data
{
    [global::System.CodeDom.Compiler.GeneratedCode("Kiota", "1.0.0")]
    #pragma warning disable CS1591
    public partial class DataPatchRequestBody : IParsable
    #pragma warning restore CS1591
    {
        /// <summary>This is used for saving server resources, when frontend has a duplicated version of the validator. The validators will be executed on process/next anyway</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public List<string>? IgnoredValidators { get; set; }
#nullable restore
#else
        public List<string> IgnoredValidators { get; set; }
#endif
        /// <summary>The patches property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public List<global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Ttd.FrontendTest.Instances.Item.Item.Data.DataPatchRequestBody_patches>? Patches { get; set; }
#nullable restore
#else
        public List<global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Ttd.FrontendTest.Instances.Item.Item.Data.DataPatchRequestBody_patches> Patches { get; set; }
#endif
        /// <summary>
        /// Creates a new instance of the appropriate class based on discriminator value
        /// </summary>
        /// <returns>A <see cref="global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Ttd.FrontendTest.Instances.Item.Item.Data.DataPatchRequestBody"/></returns>
        /// <param name="parseNode">The parse node to use to read the discriminator value and create the object</param>
        public static global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Ttd.FrontendTest.Instances.Item.Item.Data.DataPatchRequestBody CreateFromDiscriminatorValue(IParseNode parseNode)
        {
            _ = parseNode ?? throw new ArgumentNullException(nameof(parseNode));
            return new global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Ttd.FrontendTest.Instances.Item.Item.Data.DataPatchRequestBody();
        }
        /// <summary>
        /// The deserialization information for the current model
        /// </summary>
        /// <returns>A IDictionary&lt;string, Action&lt;IParseNode&gt;&gt;</returns>
        public virtual IDictionary<string, Action<IParseNode>> GetFieldDeserializers()
        {
            return new Dictionary<string, Action<IParseNode>>
            {
                { "ignoredValidators", n => { IgnoredValidators = n.GetCollectionOfPrimitiveValues<string>()?.AsList(); } },
                { "patches", n => { Patches = n.GetCollectionOfObjectValues<global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Ttd.FrontendTest.Instances.Item.Item.Data.DataPatchRequestBody_patches>(global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Ttd.FrontendTest.Instances.Item.Item.Data.DataPatchRequestBody_patches.CreateFromDiscriminatorValue)?.AsList(); } },
            };
        }
        /// <summary>
        /// Serializes information the current object
        /// </summary>
        /// <param name="writer">Serialization writer to use to serialize this model</param>
        public virtual void Serialize(ISerializationWriter writer)
        {
            _ = writer ?? throw new ArgumentNullException(nameof(writer));
            writer.WriteCollectionOfPrimitiveValues<string>("ignoredValidators", IgnoredValidators);
            writer.WriteCollectionOfObjectValues<global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Ttd.FrontendTest.Instances.Item.Item.Data.DataPatchRequestBody_patches>("patches", Patches);
        }
    }
}
#pragma warning restore CS0618
