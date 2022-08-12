using Hik.Quartz.Extensions;
using System.Xml.Serialization;

namespace Hik.Quartz.Contracts.Xml
{
    [XmlRoot(ElementName = "cron", Namespace = Namespaces.JobSchedulingData)]
    public class Cron
    {
        [XmlElement(ElementName = "name", Namespace = Namespaces.JobSchedulingData)]
        public string Name { get; set; }
        [XmlElement(ElementName = "group", Namespace = Namespaces.JobSchedulingData)]
        public string Group { get; set; }
        [XmlElement(ElementName = "description", Namespace = Namespaces.JobSchedulingData)]
        public string Description { get; set; }
        [XmlElement(ElementName = "job-name", Namespace = Namespaces.JobSchedulingData)]
        public string Jobname { get; set; }
        [XmlElement(ElementName = "job-group", Namespace = Namespaces.JobSchedulingData)]
        public string Jobgroup { get; set; }
        [XmlElement(ElementName = "job-data-map", Namespace = Namespaces.JobSchedulingData)]
        public JobDataMap Jobdatamap { get; set; }
        [XmlElement(ElementName = "misfire-instruction", Namespace = Namespaces.JobSchedulingData)]
        public string Misfireinstruction { get; set; }
        [XmlElement(ElementName = "cron-expression", Namespace = Namespaces.JobSchedulingData)]
        public string Cronexpression { get; set; }
    }
}
