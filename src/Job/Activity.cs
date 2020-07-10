using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
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

        }

        public async Task Start()
        {
            bool singleInstance;
            var instance = new Mutex(true, $@"Global\{Parameters.ClassName}_{Parameters.TriggerKey}", out singleInstance);

            if (singleInstance)
            {
                await StartProcess();
                instance.Dispose();
            }
            else
            {
                instance.Dispose();
            }
        }

        public void Kill()
        {
            if (!hostProcess.HasExited)
            {
                hostProcess.Kill();
            }
        }

        private Task StartProcess()
        {

#if DEBUG
            Type jobType = Type.GetType(Parameters.ClassName);

            Impl.JobProcessBase job = (Impl.JobProcessBase)Activator.CreateInstance(jobType, Parameters.TriggerKey, Parameters.ConfigFilePath, Parameters.ConnectionString);
            job.Parameters = Parameters;
            job.ExecuteAsync().GetAwaiter().GetResult();
            return Task.CompletedTask;

#elif RELEASE
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();

            hostProcess.StartInfo.FileName = $"{Parameters.TriggerKey}\\JobHost.exe";
            hostProcess.StartInfo.Arguments = Parameters.ToString();
            hostProcess.StartInfo.CreateNoWindow = true;
            hostProcess.StartInfo.UseShellExecute = false;
            hostProcess.StartInfo.RedirectStandardOutput = true;
            hostProcess.StartInfo.RedirectStandardError = true;

            hostProcess.OutputDataReceived += (sender, data) => Logger.Info(data.Data);
            hostProcess.ErrorDataReceived += (sender, data) => Logger.Error(data.Data);
            Logger.Info($"Starting : {Parameters}");
            hostProcess.Start();

            hostProcess.EnableRaisingEvents = true;
            hostProcess.Exited += (object sender, EventArgs e) =>
            {
                tcs.SetResult(null);
            };

            hostProcess.BeginOutputReadLine();
            hostProcess.BeginErrorReadLine();

            return tcs.Task;
#endif
        }
    }
}
