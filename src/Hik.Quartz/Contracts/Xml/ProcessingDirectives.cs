using Hik.Quartz.Extensions;
using System.Xml.Serialization;

namespace Hik.Quartz.Contracts.Xml
{
    [XmlRoot(ElementName = "processing-directives", Namespace = Namespaces.JobSchedulingData)]
    public class ProcessingDirectives
    {
        [XmlElement(ElementName = "overwrite-existing-data", Namespace = Namespaces.JobSchedulingData)]
        public string OverwriteExistingData { get; set; } = "true";
    }
}
