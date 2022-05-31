using System.Xml.Serialization;

namespace Hik.Quartz.Contracts.Xml
{
    [XmlRoot(ElementName = "job", Namespace = "http://quartznet.sourceforge.net/JobSchedulingData")]
    public class Job
    {
        [XmlElement(ElementName = "name", Namespace = "http://quartznet.sourceforge.net/JobSchedulingData")]
        public string Name { get; set; }
        [XmlElement(ElementName = "group", Namespace = "http://quartznet.sourceforge.net/JobSchedulingData")]
        public string Group { get; set; }
        [XmlElement(ElementName = "description", Namespace = "http://quartznet.sourceforge.net/JobSchedulingData")]
        public string Description { get; set; }
        [XmlElement(ElementName = "job-type", Namespace = "http://quartznet.sourceforge.net/JobSchedulingData")]
        public string Jobtype { get; set; }
        [XmlElement(ElementName = "durable", Namespace = "http://quartznet.sourceforge.net/JobSchedulingData")]
        public string Durable { get; set; }
        [XmlElement(ElementName = "recover", Namespace = "http://quartznet.sourceforge.net/JobSchedulingData")]
        public string Recover { get; set; }
    }
}
