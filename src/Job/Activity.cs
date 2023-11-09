using Hik.DataAccess;
using Job.Email;
using Serilog;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Job
{
    [ExcludeFromCodeCoverage]
    public class Activity
    {
        private readonly DbConfiguration dbConfig;
        private readonly string WorkingDirectory;

        public Parameters Parameters { get; private set; }
        public int ProcessId
        {
            get
            {
                try
                {
                    if (hostProcess != null && !hostProcess.HasExited)
                    {
                        return hostProcess.Id;
                    }
                }
                catch (InvalidOperationException)
                {
                    return 0;
                }
                return -1;
            }
        }

        public string Id => $"{Parameters.Group}.{Parameters.TriggerKey}";

        private Process hostProcess = default;

        public Activity(Parameters parameters, DbConfiguration dbConfig, string workingDirectory)
        {
            this.Parameters = parameters;
            this.dbConfig = dbConfig;
            this.WorkingDirectory = workingDirectory;
        }

        public async Task Start()
        {
            try
            {
                if (RunningActivities.Add(this))
                {
#if DEBUG
                        await RunAsTask();
#else
                        await StartProcess();
#endif
                }
                else
                {
                    Log.Warning("Cannot start, {triggerKey} is already running.", Parameters.TriggerKey);
                }

            }
            catch (Exception e)
            {
                RunningActivities.Remove(this);
                Log.Error("ErrorMsg: {errorMsg}; Trace: {trace}", "Failed to start activity", e.ToStringDemystified());
                new EmailHelper().Send(e.Message, Parameters.TriggerKey, null);
            }
        }

        public void Kill()
        {
            if (hostProcess != null && !hostProcess.HasExited)
            {
                Log.Information("Killing process manual");
                hostProcess.Kill();
            }
            else
            {
                Log.Information("No process found");
            }
            RunningActivities.Remove(this);
        }

        private Task StartProcess()
        {
            TaskCompletionSource<object> tcs = new ();
            hostProcess = new Process
            {
                StartInfo =
                {
                    FileName = $"{Parameters.Group}\\JobHost.exe",
                    Arguments = Parameters.ToString(),
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = this.WorkingDirectory,
                },
                EnableRaisingEvents = true,
            };

            hostProcess.ErrorDataReceived += new DataReceivedEventHandler(LogErrorData);
            hostProcess.Exited += (object sender, EventArgs e) =>
            {
                tcs.SetResult(null);
                if (!RunningActivities.Remove(this))
                {
                    Log.Information("Cannot remove activity from ActivityBag");
                }
            };

            hostProcess.Start();

            hostProcess.BeginOutputReadLine();
            hostProcess.BeginErrorReadLine();

            return tcs.Task;
        }

        private async Task RunAsTask()
        {
            var job = await JobFactory.GetJobAsync(Parameters, this.dbConfig, Log.Logger);
            try
            {
                await job.ExecuteAsync();
            }
            catch (Exception e)
            {
                Log.Error("ErrorMsg: {errorMsg}; Trace: {trace}", "Failed to start task", e.ToStringDemystified());
            }
            finally
            {
                RunningActivities.Remove(this);
            }
        }

        private void LogErrorData(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                Log.Error("{trigger} - {activityId} - {data}", Parameters.TriggerKey, Parameters.ActivityId, e.Data);
            }
        }
    }
}