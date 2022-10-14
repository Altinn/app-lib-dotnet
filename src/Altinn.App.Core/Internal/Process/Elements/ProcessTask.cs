using System.Xml.Serialization;
using Altinn.App.Core.Internal.Process.Elements.Base;

namespace Altinn.App.Core.Internal.Process.Elements
{
    /// <summary>
    /// Class representing the task of a process
    /// </summary>
    public class ProcessTask: FlowElement
    {
        /// <summary>
        /// Gets or sets the outgoing id of a task
        /// </summary>
        [XmlAttribute("tasktype", Namespace = "http://altinn.no")]
        public string TaskType { get; set; }

        public override string ElementType()
        {
            return "Task";
        }
    }
}
