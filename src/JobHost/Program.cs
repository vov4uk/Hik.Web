using Job;
using Job.Email;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace JobHost
{
    [ExcludeFromCodeCoverage]
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            IEmailHelper email = null;
            try
            {
                email = new EmailHelper();
                var parameters = Parameters.Parse(args);
                IJobProcess job = await JobFactory.GetJobAsync(parameters);

                await job.ExecuteAsync();
            }
            catch (Exception ex)
            {
                email?.Send(ex.Message);
                Environment.ExitCode = -1;
            }
        }
    }
}