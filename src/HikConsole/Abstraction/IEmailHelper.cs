using HikConsole.Config;

namespace HikConsole.Abstraction
{
    public interface IEmailHelper
    {
        void SendEmail(EmailConfig settings, string msg);
    }
}
