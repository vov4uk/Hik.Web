using Hik.Quartz.Extensions;
using System.Xml.Serialization;

namespace Hik.Quartz.Contracts.Xml
{
    [XmlRoot(ElementName = "entry", Namespace = Namespaces.JobSchedulingData)]
    public class Entry
    {
        [XmlElement(ElementName = "key", Namespace = Namespaces.JobSchedulingData)]
        public string Key { get; set; }
        [XmlElement(ElementName = "value", Namespace = Namespaces.JobSchedulingData)]
        public string Value { get; set; }
    }
}
