using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Mail;
using HikConsole.Abstraction;
using HikConsole.Config;

namespace HikConsole.Helpers
{
    [ExcludeFromCodeCoverage]
    public class EmailHelper : IEmailHelper
    {
        public void SendEmail(EmailConfig settings, string msg)
        {
            try
            {
                if (settings != null)
                {
                    using var mail = new MailMessage
                    {
                        From = new MailAddress(settings.UserName),
                    };
                    mail.To.Add(settings.Receiver);
                    mail.Subject = "HikConsole error";
                    mail.Body = msg;
                    mail.IsBodyHtml = false;

                    using var smtp = new SmtpClient(settings.Server, settings.Port)
                    {
                        Credentials = new NetworkCredential(settings.UserName, settings.Password),
                        EnableSsl = true,
                    };
                    smtp.Send(mail);
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error("SendEmail failed", ex);
            }
        }
    }
}