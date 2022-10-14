using System.Xml.Serialization;
using Altinn.App.Core.Internal.Process.Elements.Base;

namespace Altinn.App.Core.Internal.Process.Elements
{
    /// <summary>
    /// Represents an exclusive gateway from a BPMN process definition.
    /// </summary>
    public class ExclusiveGateway: FlowElement
    {
        /// <summary>
        /// Get or sets the default path of the exclusive gateway.
        /// </summary>
        [XmlAttribute("default")]
        public string Default { get; set; }

        public override string ElementType()
        {
            return "ExclusiveGateway";
        }
    }
}
