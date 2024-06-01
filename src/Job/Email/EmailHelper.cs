using System;
using System.Diagnostics;
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
            if (!Debugger.IsAttached)
            {
                string configPath = System.IO.Path.Combine(Environment.CurrentDirectory, "email.json");
                if (System.IO.File.Exists(configPath))
                {
                    Settings = Extensions.HikConfigExtensions.GetConfig<EmailConfig>(System.IO.File.ReadAllText(configPath));
                }
            }
        }

        public void Send(string error, string className = null, string hikJobDetails = null)
        {
            var body = BuildBody(hikJobDetails);
            var subject = $"{className ?? "Hik.Web"} {error}".Replace('\r', ' ').Replace('\n', ' ');
            Send(subject, body);
        }

        public void Send(string subject, string body)
        {
            try
            {
                if (Debugger.IsAttached)
                {
                    Logger.LogError(body);
                }
                else
                {
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
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Failed to sent email");
            }
        }

        private static string BuildBody(string hikJobDetails)
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
</body>
</html>";
        }
    }
}