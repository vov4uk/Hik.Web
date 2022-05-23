using System.Xml.Serialization;

namespace Hik.Quartz.Contracts.Xml
{
    [XmlRoot(ElementName = "job-scheduling-data", Namespace = "http://quartznet.sourceforge.net/JobSchedulingData")]
    public class JobSchedulingData
    {
        [XmlElement(ElementName = "processing-directives")]
        public ProcessingDirectives Processingdirectives { get; set; }

        [XmlElement(ElementName = "schedule", Namespace = "http://quartznet.sourceforge.net/JobSchedulingData")]
        public Schedule Schedule { get; set; }

        [XmlAttribute(AttributeName = "xmlns")]
        public string Xmlns { get; set; }

        [XmlAttribute(AttributeName = "version")]
        public string Version { get; set; }
    }
}
