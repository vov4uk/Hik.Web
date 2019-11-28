using System;
using System.ComponentModel;

namespace HikConsole.Abstraction
{
    public interface ILogger
    {
        void Trace([Localizable(false)]string message);

        void Debug([Localizable(false)]string message);

        void Error([Localizable(false)]string message);

        void Error([Localizable(false)]string message, Exception exception);

        void Info([Localizable(false)]string message);

        void Warn([Localizable(false)]string message);
    }
}
