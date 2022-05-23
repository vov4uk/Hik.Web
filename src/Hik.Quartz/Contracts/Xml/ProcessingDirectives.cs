using System.Xml.Serialization;

namespace Hik.Quartz.Contracts.Xml
{
    [XmlRoot(ElementName = "processing-directives", Namespace = "http://quartznet.sourceforge.net/JobSchedulingData")]
    public class ProcessingDirectives
    {
        [XmlElement(ElementName = "overwrite-existing-data", Namespace = "http://quartznet.sourceforge.net/JobSchedulingData")]
        public string OverwriteExistingData { get; set; }
    }
}
