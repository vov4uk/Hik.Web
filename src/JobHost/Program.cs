using System;
using Job;

namespace JobHost
{
    class Program
    {
        static void Main(string[] args)
        {
            var parameters = Parameters.Parse(args);

            System.Diagnostics.Trace.CorrelationManager.ActivityId = parameters.ActivityId;
            Type jobType = Type.GetType(parameters.ClassName);

            Job.Impl.JobProcessBase job = (Job.Impl.JobProcessBase)Activator.CreateInstance(jobType, parameters.TriggerKey, parameters.ConfigFilePath, parameters.ConnectionString);
            job.Parameters = parameters;
            job.ExecuteAsync().GetAwaiter().GetResult();
        }
    }
}
