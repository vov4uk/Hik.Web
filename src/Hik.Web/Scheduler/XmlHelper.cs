using System.IO;
using System.Xml.Serialization;

namespace Hik.Web.Scheduler
{
    public static class XmlHelper
    {
        public static JobSchedulingData GetJobSchedulingData()
        {
            var options = new QuartzOption(Program.Configuration);
            var xmlFilePath = options.Plugin.JobInitializer.FileNames;
            var xml = File.ReadAllText(xmlFilePath);

            XmlSerializer serializer = new XmlSerializer(typeof(JobSchedulingData));
            using (StringReader reader = new StringReader(xml))
            {
                return (JobSchedulingData)serializer.Deserialize(reader);
            }
        }

        public static void UpdateJobSchedulingData(JobSchedulingData data)
        {
            var options = new QuartzOption(Program.Configuration);
            var xmlFilePath = options.Plugin.JobInitializer.FileNames;

            XmlSerializer serializer = new XmlSerializer(typeof(JobSchedulingData));
            using (StringWriter writer = new StringWriter())
            {
                serializer.Serialize(writer, data);
                File.WriteAllText(xmlFilePath, writer.ToString());
            }
        }
    }
}
