﻿using Job.Email;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Job
{
    [ExcludeFromCodeCoverage]
    public class Activity
    {
        private const string JobHost = "JobHost.exe";
        protected readonly ILogger Logger;
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

            Logger = new LoggerFactory()
                .AddFile($"logs\\{parameters.TriggerKey}.txt")
                .AddSeq()
                .CreateLogger(parameters.TriggerKey);

            Logger.LogInformation("Created with parameters {parameters}.", parameters);
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
                    Logger.LogInformation("Cannot start, {triggerKey} is already running.", Parameters.TriggerKey);
                }

            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to start activity");
                email.Send(ex.Message);
            }
            finally
            {
                Logger.LogInformation("StartProcess. Done.");
            }
        }

        public void Kill()
        {
            if (hostProcess != null && !hostProcess.HasExited)
            {
                Logger.LogInformation("Killing process manual");
                hostProcess.Kill();
            }
            else
            {
                Logger.LogInformation("No process found");
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
                    FileName = JobHost,
                    Arguments = Parameters.ToString(),
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                },
                EnableRaisingEvents = true,
            };

            hostProcess.OutputDataReceived += new DataReceivedEventHandler(LogData);
            hostProcess.ErrorDataReceived += new DataReceivedEventHandler(LogErrorData);
            hostProcess.Exited += (object sender, EventArgs e) =>
            {
                tcs.SetResult(null);
                Logger.LogInformation("Process exit with code: {exitCode}", hostProcess.ExitCode);
                if (!RunningActivities.Remove(this))
                {
                    Logger.LogInformation("Cannot remove activity from ActivityBag");
                }
            };

            hostProcess.Start();

            hostProcess.BeginOutputReadLine();
            hostProcess.BeginErrorReadLine();

            return tcs.Task;
        }

        private async Task RunAsTask()
        {
            IJobProcess job = JobFactory.GetJob(Parameters, Logger, email);
            started = DateTime.Now;

            try
            {
                await job.ExecuteAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to start task");
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
                Logger.LogInformation("HasExited : {hasExited} - {data} - {sender}", (sender as Process)?.HasExited, e.Data, sender );
            }
        }

        private void LogData(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                Logger.LogInformation("{data}", e.Data);
            }
        }
    }
}