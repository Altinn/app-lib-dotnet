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
    public partial class InstanceSelection : IParsable
    #pragma warning restore CS1591
    {
        /// <summary>The defaultRowsPerPage property</summary>
        public int? DefaultRowsPerPage { get; set; }
        /// <summary>The defaultSelectedOption property</summary>
        public int? DefaultSelectedOption { get; set; }
        /// <summary>The rowsPerPageOptions property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public List<int?>? RowsPerPageOptions { get; set; }
#nullable restore
#else
        public List<int?> RowsPerPageOptions { get; set; }
#endif
        /// <summary>The sortDirection property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? SortDirection { get; set; }
#nullable restore
#else
        public string SortDirection { get; set; }
#endif
        /// <summary>
        /// Creates a new instance of the appropriate class based on discriminator value
        /// </summary>
        /// <returns>A <see cref="global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.InstanceSelection"/></returns>
        /// <param name="parseNode">The parse node to use to read the discriminator value and create the object</param>
        public static global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.InstanceSelection CreateFromDiscriminatorValue(IParseNode parseNode)
        {
            _ = parseNode ?? throw new ArgumentNullException(nameof(parseNode));
            return new global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.InstanceSelection();
        }
        /// <summary>
        /// The deserialization information for the current model
        /// </summary>
        /// <returns>A IDictionary&lt;string, Action&lt;IParseNode&gt;&gt;</returns>
        public virtual IDictionary<string, Action<IParseNode>> GetFieldDeserializers()
        {
            return new Dictionary<string, Action<IParseNode>>
            {
                { "defaultRowsPerPage", n => { DefaultRowsPerPage = n.GetIntValue(); } },
                { "defaultSelectedOption", n => { DefaultSelectedOption = n.GetIntValue(); } },
                { "rowsPerPageOptions", n => { RowsPerPageOptions = n.GetCollectionOfPrimitiveValues<int?>()?.AsList(); } },
                { "sortDirection", n => { SortDirection = n.GetStringValue(); } },
            };
        }
        /// <summary>
        /// Serializes information the current object
        /// </summary>
        /// <param name="writer">Serialization writer to use to serialize this model</param>
        public virtual void Serialize(ISerializationWriter writer)
        {
            _ = writer ?? throw new ArgumentNullException(nameof(writer));
            writer.WriteIntValue("defaultRowsPerPage", DefaultRowsPerPage);
            writer.WriteIntValue("defaultSelectedOption", DefaultSelectedOption);
            writer.WriteCollectionOfPrimitiveValues<int?>("rowsPerPageOptions", RowsPerPageOptions);
            writer.WriteStringValue("sortDirection", SortDirection);
        }
    }
}
#pragma warning restore CS0618
