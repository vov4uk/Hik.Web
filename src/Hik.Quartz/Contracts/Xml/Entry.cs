using System.Xml.Serialization;

namespace Hik.Quartz.Contracts.Xml
{
    [XmlRoot(ElementName = "entry", Namespace = "http://quartznet.sourceforge.net/JobSchedulingData")]
    public class Entry
    {
        [XmlElement(ElementName = "key", Namespace = "http://quartznet.sourceforge.net/JobSchedulingData")]
        public string Key { get; set; }
        [XmlElement(ElementName = "value", Namespace = "http://quartznet.sourceforge.net/JobSchedulingData")]
        public string Value { get; set; }
    }
}
