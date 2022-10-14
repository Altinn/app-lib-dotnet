using System.Xml.Serialization;
using Altinn.App.Core.Internal.Process.Elements.Base;

namespace Altinn.App.Core.Internal.Process.Elements
{
    /// <summary>
    /// Class representing the start event of a process
    /// </summary>
    public class StartEvent: FlowElement
    {
        public override string ElementType()
        {
            return "StartEvent";
        }
    }
}
