using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Job.Email
{
    [ExcludeFromCodeCoverage]
    public class EmailHelper : IEmailHelper
    {
        static EmailConfig Settings { get;}
        static readonly ILogger Logger = new LoggerFactory().CreateLogger(nameof(EmailHelper));

        static EmailHelper()
        {
#if RELEASE
            string configPath = System.IO.Path.Combine(Environment.CurrentDirectory, "email.json");
            Settings = Extensions.HikConfigExtensions.GetConfig<EmailConfig>(configPath);
#endif
        }

        public void Send(string error, string alias = null, string hikJobDetails = null)
        {
            string errorDetails = string.Empty;

            var body = BuildBody(errorDetails, hikJobDetails, error);
            var subject = $"{alias ?? "Hik.Web"} {error}".Replace('\r', ' ').Replace('\n', ' ');
            Send(subject, body);
        }

        public void Send(string subject, string body)
        {
            try
            {
#if DEBUG
                Logger.LogError(body);
#elif RELEASE
                if (Settings != null)
                {
                    using var mail = new System.Net.Mail.MailMessage
                    {
                        From = new System.Net.Mail.MailAddress(Settings.UserName),
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true,
                    };

                    mail.To.Add(Settings.Receiver);
                    using var smtp = new System.Net.Mail.SmtpClient(Settings.Server, Settings.Port)
                    {
                        Credentials = new System.Net.NetworkCredential(Settings.UserName, Settings.Password),
                        EnableSsl = true,
                    };
                    smtp.Send(mail);
                }
                else
                {
                    Logger.LogError("Email settings file not exist");
                }
#endif
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Failed to sent email");
            }
        }

        private static string BuildBody(string details, string hikJobDetails, string message)
        {
            return $@"<!DOCTYPE html>
<html>
<head>
<style>
table {{
      font - family: arial, sans-serif;
  border-collapse: collapse;
  width: 100%;
}}

td, th {{
      border: 1px solid #dddddd;
  text-align: left;
  padding: 8px;
}}

tr:nth-child(even) {{
      background - color: #dddddd;
}}
</style>
</head>
<body>

{hikJobDetails}

{details}

<h2>Call stack</h2>
<p>{message}</p>

</body>
</html>";
        }
    }
}