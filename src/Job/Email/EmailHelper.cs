using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Mail;
using System.Reflection;
using System.Text;
using Hik.Api;
using Newtonsoft.Json;
using NLog;

namespace Job.Email
{
    [ExcludeFromCodeCoverage]
    public static class EmailHelper
    {

        static EmailConfig Settings { get;}
        static readonly ILogger logger = LogManager.GetCurrentClassLogger();

        static EmailHelper()
        {
            string configPath = Path.Combine(GetAssemblyDirectory(), "email.json");
            Settings = JsonConvert.DeserializeObject<EmailConfig>(File.ReadAllText(configPath));
        }

        public static void Send(Exception ex, string alias = null, string hikJobDetails = null)
        {
            try
            {
                string errorDetails = string.Empty;

                if (ex is HikException)
                {
                    var hikEx = ex as HikException;
                    errorDetails = $@"<ul>
  <li>Error Code : {hikEx.ErrorCode}</li>
  <li>{hikEx.ErrorMessage}</li>
</ul>";
                }

                var msg = BuildBody(errorDetails, hikJobDetails, ex.Message, ex.ToString());

                if (Settings != null)
                {
                    using var mail = new MailMessage
                    {
                        From = new MailAddress(Settings.UserName),
                        Subject = $"{alias ?? "Hik.Web"} {(ex as HikException)?.ErrorMessage ?? ex.Message}",
                        Body = msg,
                        IsBodyHtml = true,
                    };

                    mail.To.Add(Settings.Receiver);

#if DEBUG
                    logger.Info(msg);
#elif RELEASE
                    using var smtp = new SmtpClient(Settings.Server, Settings.Port)
                    {
                        Credentials = new System.Net.NetworkCredential(Settings.UserName, Settings.Password),
                        EnableSsl = true,
                    };
                    smtp.Send(mail);
#endif
                }
            }
            catch (Exception e)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Failed to Sent Email");
                sb.AppendLine($"Parent exeption {ex.Message} - {ex.StackTrace}");
                sb.AppendLine($"Local exeption {e}");
                logger.Error(sb.ToString());
            }
        }

        private static string BuildBody(string details, string hikJobDetails, string message, string callStack)
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

<pre>
{callStack}
</pre>
</body>
</html>";
        }

        private static string GetAssemblyDirectory()
        {
            string codeBase = Assembly.GetExecutingAssembly().Location;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            return Path.GetDirectoryName(path);
        }
    }
}