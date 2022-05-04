using Hik.DataAccess;
using Hik.DataAccess.Abstractions;
using Job.Email;
using NLog;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Job
{
    [ExcludeFromCodeCoverage]
    public class Activity
    {
        protected static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
        private DateTime started = default;
        private readonly EmailHelper email = new EmailHelper();
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
            Log($"Activity. Activity created with parameters {parameters}.");
        }

        public Task Start()
        {
            try
            {
                using var instance = new Mutex(true, $@"Global\{Parameters.TriggerKey}", out bool singleInstance);
                if (singleInstance)
                {
                    Log("Activity. StartProcess...");
                    return StartProcess();
                }
                else
                {
                    Log($"Activity. Cannot start, {Parameters.TriggerKey} is already running.");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Activity.Start - catch exception : {ex}");
                email.Send(ex);
            }
            finally
            {
                Log("Activity. StartProcess. Done.");
            }
            return Task.CompletedTask;
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
            Logger.Info($"Expected Path : {Parameters.Group}\\JobHost.exe, Actual Path : {hostProcess.StartInfo.FileName}, Working Directory : {Environment.CurrentDirectory}");
            hostProcess.Start();

            hostProcess.EnableRaisingEvents = true;
            hostProcess.Exited += (object sender, EventArgs e) =>
            {
                tcs.SetResult(null);
                Log($"Activity. Process exit with code: {hostProcess.ExitCode}");
                if (!ActivityBag.Remove(this))
                {
                    Log("Cannot remove activity from ActivityBag");
                }
            };

            ActivityBag.Add(this);
            hostProcess.BeginOutputReadLine();
            hostProcess.BeginErrorReadLine();

            return tcs.Task;
        }

        private async Task RunAsTask()
        {
            Type jobType = Type.GetType(Parameters.ClassName) ?? throw new ArgumentException($"No such type exist '{Parameters.ClassName}'");
            IUnitOfWorkFactory unitOfWorkFactory = new UnitOfWorkFactory(Parameters.ConnectionString);
            IHikDatabase db = new HikDatabase(unitOfWorkFactory);

            Impl.JobProcessBase job = (Impl.JobProcessBase)Activator.CreateInstance(
                    jobType, $"{Parameters.Group}.{Parameters.TriggerKey}",
                    Parameters.ConfigFilePath,
                    db,
                    email,
                    Parameters.ActivityId);
            started = DateTime.Now;
            ActivityBag.Add(this);
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
                ActivityBag.Remove(this);
            }
        }

        private void LogErrorData(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                Guid prevId = Trace.CorrelationManager.ActivityId;
                Trace.CorrelationManager.ActivityId = this.Id;
                Logger.Info($"HasExited : {(sender as Process)?.HasExited} - {e.Data} - {sender}");
                Trace.CorrelationManager.ActivityId = prevId;
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