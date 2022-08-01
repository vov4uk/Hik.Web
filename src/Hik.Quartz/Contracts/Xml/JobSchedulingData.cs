using Hik.Quartz.Extensions;
using System.Xml.Serialization;

namespace Hik.Quartz.Contracts.Xml
{
    [XmlRoot(ElementName = "job-scheduling-data", Namespace = Namespaces.JobSchedulingData)]
    public class JobSchedulingData
    {
        [XmlElement(ElementName = "processing-directives")]
        public ProcessingDirectives Processingdirectives { get; set; } = new ProcessingDirectives();

        [XmlElement(ElementName = "schedule", Namespace = Namespaces.JobSchedulingData)]
        public Schedule Schedule { get; set; } = new Schedule();

        [XmlAttribute(AttributeName = "xmlns")]
        public string Xmlns { get; set; }

        [XmlAttribute(AttributeName = "version")]
        public string Version { get; set; } = "2.0";
    }
}
