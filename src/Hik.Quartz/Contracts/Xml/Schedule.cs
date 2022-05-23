using System.Collections.Generic;
using System.Xml.Serialization;

namespace Hik.Quartz.Contracts.Xml
{
    [XmlRoot(ElementName = "schedule", Namespace = "http://quartznet.sourceforge.net/JobSchedulingData")]
    public class Schedule
    {
        [XmlElement(ElementName = "job", Namespace = "http://quartznet.sourceforge.net/JobSchedulingData")]
        public Job Job { get; set; }
        [XmlElement(ElementName = "trigger", Namespace = "http://quartznet.sourceforge.net/JobSchedulingData")]
        public List<Trigger> Trigger { get; set; }
    }
}
