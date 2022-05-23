using System.Xml.Serialization;

namespace Hik.Quartz.Contracts.Xml
{
    [XmlRoot(ElementName = "trigger", Namespace = "http://quartznet.sourceforge.net/JobSchedulingData")]
    public class Trigger
    {
        [XmlElement(ElementName = "cron", Namespace = "http://quartznet.sourceforge.net/JobSchedulingData")]
        public Cron Cron { get; set; }
    }
}
