using System.Diagnostics.CodeAnalysis;
using HikConsole.Abstraction;

namespace HikConsole.Helpers
{
    [ExcludeFromCodeCoverage]
    public class ProgressBarFactory : IProgressBarFactory
    {
        public IProgressBar Create()
        {
            return new ProgressBar();
        }
    }
}
