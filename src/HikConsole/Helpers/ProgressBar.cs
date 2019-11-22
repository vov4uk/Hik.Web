using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;
using HikConsole.Abstraction;

namespace HikConsole.Helpers
{
    /// <summary>
    /// https://gist.github.com/DanielSWolf/0ab6a96899cc5377bf54
    /// Author.2019
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal class ProgressBar : IProgressBar
    {
        private const string Animation = @"|/-\";
        private const int BlockCount = 20;
        private readonly TimeSpan animationInterval = TimeSpan.FromSeconds(1.0 / 8);

        private readonly Timer timer;
        private readonly object timerLock = new object();

        private double currentProgress = 0;
        private string currentText = string.Empty;
        private bool disposed = false;
        private int animationIndex = 0;

        public ProgressBar()
        {
            this.timer = new Timer(this.TimerHandler);

            // A progress bar is only for temporary display in a console window.
            // If the console output is redirected to a file, draw nothing.
            // Otherwise, we'll end up with a lot of garbage in the target file.
            if (!Console.IsOutputRedirected)
            {
                this.ResetTimer();
            }
        }

        public void Report(double value)
        {
            // Make sure value is in [0..1] range
            value = Math.Max(0, Math.Min(1, value));
            Interlocked.Exchange(ref this.currentProgress, value);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            lock (this.timerLock)
            {
                this.disposed = true;
                this.UpdateText(string.Empty);
                this.timer.Dispose();
            }
        }

        private void TimerHandler(object state)
        {
            lock (this.timerLock)
            {
                if (this.disposed)
                {
                    return;
                }

                int progressBlockCount = (int)(this.currentProgress * BlockCount);
                int percent = (int)(this.currentProgress * 100);
                string text = $"[{new string('#', progressBlockCount)}" +
                    $"{new string('-', count: BlockCount - progressBlockCount)}] {percent,3}% {Animation[this.animationIndex++ % Animation.Length]}";
                this.UpdateText(text);

                this.ResetTimer();
            }
        }

        private void UpdateText(string text)
        {
            // Get length of common portion
            int commonPrefixLength = 0;
            int commonLength = Math.Min(this.currentText.Length, text.Length);
            while (commonPrefixLength < commonLength && text[commonPrefixLength] == this.currentText[commonPrefixLength])
            {
                commonPrefixLength++;
            }

            // Backtrack to the first differing character
            StringBuilder outputBuilder = new StringBuilder();
            outputBuilder.Append('\b', this.currentText.Length - commonPrefixLength);

            // Output new suffix
            outputBuilder.Append(text.Substring(commonPrefixLength));

            // If the new text is shorter than the old one: delete overlapping characters
            int overlapCount = this.currentText.Length - text.Length;
            if (overlapCount > 0)
            {
                outputBuilder.Append(' ', overlapCount);
                outputBuilder.Append('\b', overlapCount);
            }

            Console.Write(outputBuilder);
            this.currentText = text;
        }

        private void ResetTimer()
        {
            this.timer.Change(this.animationInterval, TimeSpan.FromMilliseconds(-1));
        }
    }
}
