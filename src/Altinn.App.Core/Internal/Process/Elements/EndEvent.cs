using System.Xml.Serialization;
using Altinn.App.Core.Internal.Process.Elements.Base;

namespace Altinn.App.Core.Internal.Process.Elements
{
    /// <summary>
    /// Class representing the end event of a process
    /// </summary>
    public class EndEvent: FlowElement
    {
        public override string ElementType()
        {
            return "EndEvent";
        }
    }
}
