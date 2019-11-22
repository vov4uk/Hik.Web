using System;

namespace HikConsole.Abstraction
{
    public interface IProgressBar : IDisposable, IProgress<double>
    {
    }
}
