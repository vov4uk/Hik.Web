using Autofac;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Xml.Serialization;

namespace Hik.Web.Scheduler
{
    public class XmlHelper
    {
        private static string xmlFilePath;

        public static JobSchedulingData GetJobSchedulingData()
        {
            var configuration = AutofacConfig.Container.Resolve<IConfiguration>();
            var options = new QuartzOption(configuration);
            xmlFilePath = options.Plugin.JobInitializer.FileNames;
            var xml = File.ReadAllText(xmlFilePath);

            XmlSerializer serializer = new XmlSerializer(typeof(JobSchedulingData));
            using (StringReader reader = new StringReader(xml))
            {
                return (JobSchedulingData)serializer.Deserialize(reader);
            }
        }

        public static void UpdateJobSchedulingData(JobSchedulingData data)
        {
            var configuration = AutofacConfig.Container.Resolve<IConfiguration>();
            var options = new QuartzOption(configuration);
            xmlFilePath = options.Plugin.JobInitializer.FileNames;

            XmlSerializer serializer = new XmlSerializer(typeof(JobSchedulingData));
            using (StringWriter writer = new StringWriter())
            {
                serializer.Serialize(writer, data);
                File.WriteAllText(xmlFilePath, writer.ToString());
            }
        }
    }
}
