using Hik.DataAccess;
using Hik.Helpers.Email;
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
        private readonly EmailConfig email;
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

        public Activity(Parameters parameters, DbConfiguration dbConfig, EmailConfig email, string workingDirectory)
        {
            this.Parameters = parameters;
            this.dbConfig = dbConfig;
            this.email = email;
            this.WorkingDirectory = workingDirectory;
        }

        public async Task Start()
        {
            try
            {
                if (RunningActivities.Add(this))
                {
#if DEBUG
                    var job = await JobFactory.GetJobAsync(Parameters, this.dbConfig, this.email, Log.Logger);
                    await job.ExecuteAsync();
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
                Log.Error("ErrorMsg: {errorMsg}; Trace: {trace}", "Failed to start activity", e.ToStringDemystified());
            }
            finally
            {
                if (!RunningActivities.Remove(this))
                {
                    Log.Information("Cannot remove {ActivityId} from ActivityBag ", $"{Parameters.TriggerKey}_{Parameters.ActivityId}");
                }
                else
                {
                    Log.Information("{ActivityId} finallized", $"{Parameters.TriggerKey}_{Parameters.ActivityId}");
                }
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

        private Task<object> StartProcess()
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
            };

            hostProcess.Start();

            hostProcess.BeginOutputReadLine();
            hostProcess.BeginErrorReadLine();

            return tcs.Task;
        }

        private void LogErrorData(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                Log.Error("{ActivityId} - {data}", $"{Parameters.TriggerKey}_{Parameters.ActivityId}", e.Data);
            }
        }
    }
}