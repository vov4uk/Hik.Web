using System.Collections.Generic;
using System.Xml.Serialization;

namespace Hik.Quartz.Contracts.Xml
{
    [XmlRoot(ElementName = "job-data-map", Namespace = "http://quartznet.sourceforge.net/JobSchedulingData")]
    public class JobDataMap
    {
        [XmlElement(ElementName = "entry", Namespace = "http://quartznet.sourceforge.net/JobSchedulingData")]
        public List<Entry> Entry { get; set; }
    }
}
