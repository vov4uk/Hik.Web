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
        private static readonly EmailHelper email = new EmailHelper();
        private DateTime started = default;
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

        public DateTime StartTime => hostProcess?.StartTime ?? started;

        private Process hostProcess = default;

        public Activity(Parameters parameters)
        {
            parameters.ActivityId = Guid.NewGuid();
            Parameters = parameters;
        }

        public async Task Start()
        {
            try
            {
                if (RunningActivities.Add(this))
                {
                    if (Parameters.RunAsTask)
                    {
                        await RunAsTask();
                    }
                    else
                    {
                        await StartProcess();
                    }
                }
                else
                {
                    Log.Information("Cannot start, {triggerKey} is already running.", Parameters.TriggerKey);
                }

            }
            catch (Exception e)
            {
                Log.Error("ErrorMsg: {errorMsg}; Trace: {trace}", "Failed to start activity", e.ToStringDemystified());
                email.Send(e.Message);
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
            IJobProcess job = JobFactory.GetJob(Parameters, email);
            started = DateTime.Now;

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
                Log.Information("HasExited : {hasExited} - {data} - {sender}", (sender as Process)?.HasExited, e.Data, sender );
            }
        }
    }
}