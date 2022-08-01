using Hik.Quartz.Extensions;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Hik.Quartz.Contracts.Xml
{
    [XmlRoot(ElementName = "schedule", Namespace = Namespaces.JobSchedulingData)]
    public class Schedule
    {
        [XmlElement(ElementName = "job", Namespace = Namespaces.JobSchedulingData)]
        public Job Job { get; set; } = new Job();
        [XmlElement(ElementName = "trigger", Namespace = Namespaces.JobSchedulingData)]
        public List<Trigger> Trigger { get; set; } = new List<Trigger>();
    }
}
