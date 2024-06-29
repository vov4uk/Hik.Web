using Serilog;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Hik.Helpers.Email
{
    [ExcludeFromCodeCoverage]
    public class EmailHelper : IEmailHelper
    {
        private EmailConfig Settings { get; }

        public EmailHelper(EmailConfig settings)
        {
            Settings = settings;
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
                    Log.Error(body);
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
                        Log.Error("Email settings file not exist");
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Failed to sent email");
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