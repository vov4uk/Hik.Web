using System.Collections.Generic;
using System.Xml.Serialization;

namespace Hik.Web.Scheduler
{
    [XmlRoot(ElementName = "processing-directives", Namespace = "http://quartznet.sourceforge.net/JobSchedulingData")]
    public class ProcessingDirectives
    {
        [XmlElement(ElementName = "overwrite-existing-data", Namespace = "http://quartznet.sourceforge.net/JobSchedulingData")]
        public string OverwriteExistingData { get; set; }
    }

    [XmlRoot(ElementName = "job", Namespace = "http://quartznet.sourceforge.net/JobSchedulingData")]
    public class Job
    {
        [XmlElement(ElementName = "name", Namespace = "http://quartznet.sourceforge.net/JobSchedulingData")]
        public string Name { get; set; }
        [XmlElement(ElementName = "group", Namespace = "http://quartznet.sourceforge.net/JobSchedulingData")]
        public string Group { get; set; }
        [XmlElement(ElementName = "description", Namespace = "http://quartznet.sourceforge.net/JobSchedulingData")]
        public string Description { get; set; }
        [XmlElement(ElementName = "job-type", Namespace = "http://quartznet.sourceforge.net/JobSchedulingData")]
        public string Jobtype { get; set; }
        [XmlElement(ElementName = "durable", Namespace = "http://quartznet.sourceforge.net/JobSchedulingData")]
        public string Durable { get; set; }
        [XmlElement(ElementName = "recover", Namespace = "http://quartznet.sourceforge.net/JobSchedulingData")]
        public string Recover { get; set; }
    }

    [XmlRoot(ElementName = "entry", Namespace = "http://quartznet.sourceforge.net/JobSchedulingData")]
    public class Entry
    {
        [XmlElement(ElementName = "key", Namespace = "http://quartznet.sourceforge.net/JobSchedulingData")]
        public string Key { get; set; }
        [XmlElement(ElementName = "value", Namespace = "http://quartznet.sourceforge.net/JobSchedulingData")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "job-data-map", Namespace = "http://quartznet.sourceforge.net/JobSchedulingData")]
    public class JobDataMap
    {
        [XmlElement(ElementName = "entry", Namespace = "http://quartznet.sourceforge.net/JobSchedulingData")]
        public List<Entry> Entry { get; set; }
    }

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

    [XmlRoot(ElementName = "trigger", Namespace = "http://quartznet.sourceforge.net/JobSchedulingData")]
    public class Trigger
    {
        [XmlElement(ElementName = "cron", Namespace = "http://quartznet.sourceforge.net/JobSchedulingData")]
        public Cron Cron { get; set; }
    }

    [XmlRoot(ElementName = "schedule", Namespace = "http://quartznet.sourceforge.net/JobSchedulingData")]
    public class Schedule
    {
        [XmlElement(ElementName = "job", Namespace = "http://quartznet.sourceforge.net/JobSchedulingData")]
        public Job Job { get; set; }
        [XmlElement(ElementName = "trigger", Namespace = "http://quartznet.sourceforge.net/JobSchedulingData")]
        public List<Trigger> Trigger { get; set; }
    }

    [XmlRoot(ElementName = "job-scheduling-data", Namespace = "http://quartznet.sourceforge.net/JobSchedulingData")]
    public class JobSchedulingData
    {
        [XmlElement(ElementName = "processing-directives")]
        public ProcessingDirectives Processingdirectives { get; set; }

        [XmlElement(ElementName = "schedule", Namespace = "http://quartznet.sourceforge.net/JobSchedulingData")]
        public Schedule Schedule { get; set; }

        [XmlAttribute(AttributeName = "xmlns")]
        public string Xmlns { get; set; }

        [XmlAttribute(AttributeName = "version")]
        public string Version { get; set; }
    }
}
