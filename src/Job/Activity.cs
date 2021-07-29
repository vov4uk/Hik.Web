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
        protected static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
        private readonly ActivityBag bag;
        private DateTime started = default;

        public readonly Guid Id;
        public Parameters Parameters { get; private set; }
        public int ProcessId => hostProcess?.Id ?? -1;

        public DateTime StartTime => hostProcess?.StartTime ?? started;

        private Process hostProcess = default;

        public Activity(Parameters parameters)
        {
            Id = Guid.NewGuid();
            Parameters = parameters;
            Parameters.ActivityId = Id;
            bag = new ActivityBag();

            Log($"Activity. Activity created with parameters {parameters}.");
        }

        public async Task Start()
        {
            try
            {
                using var instance = new Mutex(true, $@"Global\{Parameters.TriggerKey}", out bool singleInstance);
                if (singleInstance)
                {
                    Log("Activity. StartProcess...");
                    await StartProcess();
                    Log("Activity. StartProcess. Done.");
                }
                else
                {
                    Log($"Activity. Cannot start, {Parameters.TriggerKey} is already running.");
                }
            }
            catch (Exception ex) {

                Logger.Error($"Activity.Start - catch exception : {ex}");
                EmailHelper.Send(ex);
            }
        }

        public void Kill()
        {
            if (!hostProcess.HasExited)
            {
                Log("Killing process manual");
                hostProcess.Kill();
            }
        }

        private Task StartProcess()
        {
            if (Parameters.RunAsTask)
            {
                Logger.Info($"Activity. Starting Task: {Parameters}");
                return RunAsTask();
            }

            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            hostProcess = new Process
            {
                StartInfo =
                {
                    FileName = $"{Parameters.Group}\\JobHost.exe",
                    Arguments = Parameters.ToString(),
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            hostProcess.OutputDataReceived += new DataReceivedEventHandler(LogData);
            hostProcess.ErrorDataReceived += new DataReceivedEventHandler(LogErrorData);
            Logger.Info($"Activity. Starting Process: {Parameters}");
            Logger.Info($"Expected Path : {Parameters.Group}\\JobHost.exe, Actual Path : {hostProcess.StartInfo.FileName}, Working Direktory : {hostProcess.StartInfo.WorkingDirectory}");
            hostProcess.Start();

            hostProcess.EnableRaisingEvents = true;
            hostProcess.Exited += (object sender, EventArgs e) =>
            {
                tcs.SetResult(null);
                Log($"Activity. Process exit with code: {hostProcess.ExitCode}");
                if (!bag.Remove(this))
                {
                    Log("Cannot remove activity from ActivityBag");
                }
            };

            bag.Add(this);
            hostProcess.BeginOutputReadLine();
            hostProcess.BeginErrorReadLine();

            return tcs.Task;

        }

        private async Task RunAsTask()
        {
            Type jobType = Type.GetType(Parameters.ClassName);

            if (jobType == null)
            {
                throw new ArgumentException($"No such type exist '{Parameters.ClassName}'");
            }

            Impl.JobProcessBase job = (Impl.JobProcessBase)Activator.CreateInstance(jobType, $"{Parameters.Group}.{Parameters.TriggerKey}", Parameters.ConfigFilePath, Parameters.ConnectionString, Parameters.ActivityId);
            started = DateTime.Now;
            bag.Add(this);
            try
            {
                await job.ExecuteAsync();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            finally
            {
                bag.Remove(this);
            }
        }

        private void LogErrorData(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                Guid prevId = Trace.CorrelationManager.ActivityId;
                Trace.CorrelationManager.ActivityId = this.Id;
                Logger.Error($"HasExited : {(sender as Process)?.HasExited} - {e.Data} - {sender}");
                Trace.CorrelationManager.ActivityId = prevId;
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

        private void Log(string str)
        {
            Guid prevId = Trace.CorrelationManager.ActivityId;
            Trace.CorrelationManager.ActivityId = this.Id;

            Logger.Info($"{Parameters.TriggerKey} - {str}");

            Trace.CorrelationManager.ActivityId = prevId;
        }
    }
}
