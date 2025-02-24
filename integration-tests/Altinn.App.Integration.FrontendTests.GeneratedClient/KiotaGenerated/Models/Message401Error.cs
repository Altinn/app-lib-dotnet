// <auto-generated/>
#pragma warning disable CS0618
using Microsoft.Kiota.Abstractions.Extensions;
using Microsoft.Kiota.Abstractions.Serialization;
using Microsoft.Kiota.Abstractions;
using System.Collections.Generic;
using System.IO;
using System;
namespace Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models
{
    [global::System.CodeDom.Compiler.GeneratedCode("Kiota", "1.0.0")]
    #pragma warning disable CS1591
    public partial class Message401Error : ApiException, IParsable
    #pragma warning restore CS1591
    {
        /// <summary>The detail property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? Detail { get; set; }
#nullable restore
#else
        public string Detail { get; set; }
#endif
        /// <summary>The instance property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? Instance { get; set; }
#nullable restore
#else
        public string Instance { get; set; }
#endif
        /// <summary>The primary error message.</summary>
        public override string Message { get => base.Message; }
        /// <summary>The status property</summary>
        public int? Status { get; set; }
        /// <summary>The title property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? Title { get; set; }
#nullable restore
#else
        public string Title { get; set; }
#endif
        /// <summary>The type property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? Type { get; set; }
#nullable restore
#else
        public string Type { get; set; }
#endif
        /// <summary>
        /// Creates a new instance of the appropriate class based on discriminator value
        /// </summary>
        /// <returns>A <see cref="global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.Message401Error"/></returns>
        /// <param name="parseNode">The parse node to use to read the discriminator value and create the object</param>
        public static global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.Message401Error CreateFromDiscriminatorValue(IParseNode parseNode)
        {
            _ = parseNode ?? throw new ArgumentNullException(nameof(parseNode));
            return new global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.Message401Error();
        }
        /// <summary>
        /// The deserialization information for the current model
        /// </summary>
        /// <returns>A IDictionary&lt;string, Action&lt;IParseNode&gt;&gt;</returns>
        public virtual IDictionary<string, Action<IParseNode>> GetFieldDeserializers()
        {
            return new Dictionary<string, Action<IParseNode>>
            {
                { "detail", n => { Detail = n.GetStringValue(); } },
                { "instance", n => { Instance = n.GetStringValue(); } },
                { "status", n => { Status = n.GetIntValue(); } },
                { "title", n => { Title = n.GetStringValue(); } },
                { "type", n => { Type = n.GetStringValue(); } },
            };
        }
        /// <summary>
        /// Serializes information the current object
        /// </summary>
        /// <param name="writer">Serialization writer to use to serialize this model</param>
        public virtual void Serialize(ISerializationWriter writer)
        {
            _ = writer ?? throw new ArgumentNullException(nameof(writer));
            writer.WriteStringValue("detail", Detail);
            writer.WriteStringValue("instance", Instance);
            writer.WriteIntValue("status", Status);
            writer.WriteStringValue("title", Title);
            writer.WriteStringValue("type", Type);
        }
    }
}
#pragma warning restore CS0618
