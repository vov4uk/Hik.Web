using System.Xml.Serialization;

namespace Hik.Quartz.Contracts.Xml
{
    [XmlRoot(ElementName = "cron", Namespace = "http://quartznet.sourceforge.net/JobSchedulingData")]
    public class Cron
    {
        [XmlElement(ElementName = "name", Namespace = "http://quartznet.sourceforge.net/JobSchedulingData")]
        public string Name { get; set; }
        [XmlElement(ElementName = "group", Namespace = "http://quartznet.sourceforge.net/JobSchedulingData")]
        public string Group { get; set; }
        [XmlElement(ElementName = "description", Namespace = "http://quartznet.sourceforge.net/JobSchedulingData")]
        public string Description { get; set; }
        [XmlElement(ElementName = "job-name", Namespace = "http://quartznet.sourceforge.net/JobSchedulingData")]
        public string Jobname { get; set; }
        [XmlElement(ElementName = "job-group", Namespace = "http://quartznet.sourceforge.net/JobSchedulingData")]
        public string Jobgroup { get; set; }
        [XmlElement(ElementName = "job-data-map", Namespace = "http://quartznet.sourceforge.net/JobSchedulingData")]
        public JobDataMap Jobdatamap { get; set; }
        [XmlElement(ElementName = "misfire-instruction", Namespace = "http://quartznet.sourceforge.net/JobSchedulingData")]
        public string Misfireinstruction { get; set; }
        [XmlElement(ElementName = "cron-expression", Namespace = "http://quartznet.sourceforge.net/JobSchedulingData")]
        public string Cronexpression { get; set; }
    }
}
