using Hik.Quartz.Extensions;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Hik.Quartz.Contracts.Xml
{
    [XmlRoot(ElementName = "job-data-map", Namespace = Namespaces.JobSchedulingData)]
    public class JobDataMap
    {
        [XmlElement(ElementName = "entry", Namespace = Namespaces.JobSchedulingData)]
        public List<Entry> Entry { get; set; }
    }
}
