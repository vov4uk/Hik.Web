using Hik.Quartz.Extensions;
using System.Xml.Serialization;

namespace Hik.Quartz.Contracts.Xml
{
    [XmlRoot(ElementName = "trigger", Namespace = Namespaces.JobSchedulingData)]
    public class Trigger
    {
        [XmlElement(ElementName = "cron", Namespace = Namespaces.JobSchedulingData)]
        public Cron Cron { get; set; }
    }
}
