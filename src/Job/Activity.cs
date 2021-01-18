using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Job.Email;
using NLog;

namespace Job
{
    public class Activity
    {
        protected static readonly ILogger logger = LogManager.GetCurrentClassLogger();

        public readonly Guid Id;
        public Parameters Parameters { get; private set; }
        public int ProcessId
        {
            get { return hostProcess.Id; }
        }

        public DateTime StartTime
        {
            get { return hostProcess.StartTime; }
        }

        private Process hostProcess;

        public Activity(Parameters parameters)
        {
            Id = Guid.NewGuid();
            Parameters = parameters;
            Parameters.ActivityId = Id;
            hostProcess = new Process();

            Log("Activity. Activity created with parameters {0}.", parameters.ToString());
        }

        public async Task Start()
        {
            try
            {
                bool singleInstance;
                using (var instance = new Mutex(true, $@"Global\{Parameters.TriggerKey}", out singleInstance))
                {
                    if (singleInstance)
                    {
                        Log("Activity. StartProcess...");
                        await StartProcess();
                        Log("Activity. StartProcess. Done.");
                    }
                    else
                    {
                        Log("Activity. Cannot start, {0} is already running.", Parameters.TriggerKey);
                    }
                }
            }
            catch (Exception ex) {
                logger.Error($"Activity.Start - catch exception : {ex.Message}");
                logger.Error($"Activity.Start - catch exception : {ex.StackTrace}");
                EmailHelper.Send(ex);
            }
        }

        public void Kill()
        {
            if (!hostProcess.HasExited)
            {
                Log("Killing process manualy");
                hostProcess.Kill();
            }
        }

        private Task StartProcess()
        {

#if DEBUG
            Type jobType = Type.GetType(Parameters.ClassName);

            if (jobType == null)
            {
                throw new ArgumentException($"No such type exist '{Parameters.ClassName}'");
            }

            Impl.JobProcessBase job = (Impl.JobProcessBase)Activator.CreateInstance(jobType, $"{Parameters.Group}.{Parameters.TriggerKey}", Parameters.ConfigFilePath, Parameters.ConnectionString, Parameters.ActivityId);
            job.Parameters = Parameters;
            job.ExecuteAsync().GetAwaiter().GetResult();
            return Task.CompletedTask;

#elif RELEASE
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();

            hostProcess.StartInfo.FileName = $"{Parameters.Group}\\JobHost.exe";
            hostProcess.StartInfo.Arguments = Parameters.ToString();
            hostProcess.StartInfo.CreateNoWindow = true;
            hostProcess.StartInfo.UseShellExecute = false;
            hostProcess.StartInfo.RedirectStandardOutput = true;
            hostProcess.StartInfo.RedirectStandardError = true;

            hostProcess.OutputDataReceived += new DataReceivedEventHandler(LogData);
            hostProcess.ErrorDataReceived += new DataReceivedEventHandler(LogErrorData);
            logger.Info($"Activity. Starting : {Parameters}");
            hostProcess.Start();

            hostProcess.EnableRaisingEvents = true;
            hostProcess.Exited += (object sender, EventArgs e) =>
            {
                tcs.SetResult(null);
                Log("Activity. Process exit with code: {0}", hostProcess.ExitCode.ToString());
            };

            hostProcess.BeginOutputReadLine();
            hostProcess.BeginErrorReadLine();

            return tcs.Task;
#endif
        }

        private static void LogErrorData(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                logger.Error($"ErrorDataReceived : {e.Data}");
                logger.Error($"ErrorDataReceived : {sender}");
                logger.Error($"ErrorDataReceived : HasExited : {(sender as Process)?.HasExited}");
                EmailHelper.Send(new Exception(e.Data));
            }
        }
        private void LogData(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                Log(e.Data);
            }
        }

        private void Log(string format, params string[] args)
        {
            Guid prevId = Trace.CorrelationManager.ActivityId;
            Trace.CorrelationManager.ActivityId = this.Id;

            logger.Info(string.Format(format, args));

            Trace.CorrelationManager.ActivityId = prevId;
        }
    }
}
