using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Job
{
    public class Activity
    {
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
        private ActivityBag bag;

        public Activity(Parameters parameters)
        {
            Id = Guid.NewGuid();
            Parameters = parameters;
            Parameters.ActivityId = Id;
            hostProcess = new Process();
            bag = new ActivityBag();

        }

        public async Task Start()
        {
            bool singleInstance;
            var instance = new Mutex(true, string.Format(@"Global\{0}", Parameters.SingletonKey), out singleInstance);

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
            if (hostProcess.HasExited)
            {

            }
            else
            {
                hostProcess.Kill();
            }
        }

        private Task StartProcess()
        {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();

            hostProcess.StartInfo.FileName = @"D:\HikConsole\src\JobHost\bin\Release\netcoreapp3.1\publish\JobHost.exe";
            hostProcess.StartInfo.Arguments = Parameters.ToString();
            hostProcess.StartInfo.CreateNoWindow = true;
            hostProcess.StartInfo.UseShellExecute = false;
            hostProcess.StartInfo.RedirectStandardOutput = true;
            hostProcess.StartInfo.RedirectStandardError = true;

            hostProcess.OutputDataReceived += (sender, data) => Console.WriteLine(data.Data);
            hostProcess.ErrorDataReceived += (sender, data) => Console.WriteLine(data.Data);
            Console.WriteLine("starting");
            hostProcess.Start();

            hostProcess.EnableRaisingEvents = true;
            hostProcess.Exited += (object sender, EventArgs e) =>
            {
                tcs.SetResult(null);


                if (!bag.Remove(this))
                {

                }

            };

            bag.Add(this);
            hostProcess.BeginOutputReadLine();
            hostProcess.BeginErrorReadLine();

            return tcs.Task;
        }
    }
}
