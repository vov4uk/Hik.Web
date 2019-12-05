using System;
using System.Diagnostics.CodeAnalysis;
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
                    using (MailMessage mail = new MailMessage())
                    {
                        mail.From = new MailAddress(settings.UserName);
                        mail.To.Add(settings.Receiver);
                        mail.Subject = "HikConsole error";
                        mail.Body = msg;
                        mail.IsBodyHtml = false;

                        using (SmtpClient smtp = new SmtpClient(settings.Server, settings.Port))
                        {
                            smtp.Credentials = new System.Net.NetworkCredential(settings.UserName, settings.Password);
                            smtp.EnableSsl = true;
                            smtp.Send(mail);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"SendEmail failed", ex);
            }
        }
    }
}
