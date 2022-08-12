using Hik.Quartz.Extensions;
using System.Xml.Serialization;

namespace Hik.Quartz.Contracts.Xml
{
    [XmlRoot(ElementName = "job", Namespace = Namespaces.JobSchedulingData)]
    public class Job
    {
        [XmlElement(ElementName = "name", Namespace = Namespaces.JobSchedulingData)]
        public string Name { get; set; } = "Launcher";
        [XmlElement(ElementName = "group", Namespace = Namespaces.JobSchedulingData)]
        public string Group { get; set; }
        [XmlElement(ElementName = "description", Namespace = Namespaces.JobSchedulingData)]
        public string Description { get; set; } = "";
        [XmlElement(ElementName = "job-type", Namespace = Namespaces.JobSchedulingData)]
        public string Jobtype { get; set; } = "Hik.Web.Scheduler.CronTrigger, Hik.Web";
        [XmlElement(ElementName = "durable", Namespace = Namespaces.JobSchedulingData)]
        public string Durable { get; set; } = "true";
        [XmlElement(ElementName = "recover", Namespace = Namespaces.JobSchedulingData)]
        public string Recover { get; set; } = "false";
    }
}
