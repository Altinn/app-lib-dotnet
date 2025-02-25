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
    public partial class AppProcessState : IParsable
    #pragma warning restore CS1591
    {
        /// <summary>The actions property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.AppProcessState_actions? Actions { get; set; }
#nullable restore
#else
        public global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.AppProcessState_actions Actions { get; set; }
#endif
        /// <summary>The currentTask property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.ProcessElementInfo? CurrentTask { get; set; }
#nullable restore
#else
        public global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.ProcessElementInfo CurrentTask { get; set; }
#endif
        /// <summary>The ended property</summary>
        public DateTimeOffset? Ended { get; set; }
        /// <summary>The endEvent property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? EndEvent { get; set; }
#nullable restore
#else
        public string EndEvent { get; set; }
#endif
        /// <summary>The started property</summary>
        public DateTimeOffset? Started { get; set; }
        /// <summary>The startEvent property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? StartEvent { get; set; }
#nullable restore
#else
        public string StartEvent { get; set; }
#endif
        /// <summary>
        /// Creates a new instance of the appropriate class based on discriminator value
        /// </summary>
        /// <returns>A <see cref="global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.AppProcessState"/></returns>
        /// <param name="parseNode">The parse node to use to read the discriminator value and create the object</param>
        public static global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.AppProcessState CreateFromDiscriminatorValue(IParseNode parseNode)
        {
            _ = parseNode ?? throw new ArgumentNullException(nameof(parseNode));
            return new global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.AppProcessState();
        }
        /// <summary>
        /// The deserialization information for the current model
        /// </summary>
        /// <returns>A IDictionary&lt;string, Action&lt;IParseNode&gt;&gt;</returns>
        public virtual IDictionary<string, Action<IParseNode>> GetFieldDeserializers()
        {
            return new Dictionary<string, Action<IParseNode>>
            {
                { "actions", n => { Actions = n.GetObjectValue<global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.AppProcessState_actions>(global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.AppProcessState_actions.CreateFromDiscriminatorValue); } },
                { "currentTask", n => { CurrentTask = n.GetObjectValue<global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.ProcessElementInfo>(global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.ProcessElementInfo.CreateFromDiscriminatorValue); } },
                { "endEvent", n => { EndEvent = n.GetStringValue(); } },
                { "ended", n => { Ended = n.GetDateTimeOffsetValue(); } },
                { "startEvent", n => { StartEvent = n.GetStringValue(); } },
                { "started", n => { Started = n.GetDateTimeOffsetValue(); } },
            };
        }
        /// <summary>
        /// Serializes information the current object
        /// </summary>
        /// <param name="writer">Serialization writer to use to serialize this model</param>
        public virtual void Serialize(ISerializationWriter writer)
        {
            _ = writer ?? throw new ArgumentNullException(nameof(writer));
            writer.WriteObjectValue<global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.AppProcessState_actions>("actions", Actions);
            writer.WriteObjectValue<global::Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models.ProcessElementInfo>("currentTask", CurrentTask);
            writer.WriteDateTimeOffsetValue("ended", Ended);
            writer.WriteStringValue("endEvent", EndEvent);
            writer.WriteDateTimeOffsetValue("started", Started);
            writer.WriteStringValue("startEvent", StartEvent);
        }
    }
}
#pragma warning restore CS0618
