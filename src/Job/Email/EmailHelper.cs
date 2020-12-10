using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Reflection;
using HikConsole.Config;
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

        public static void Send(Exception ex)
        {
            try
            {
                var camera = GetCameraConfig(ex);
                string errorDetails = string.Empty;

                if (ex is HikApi.HikException)
                {
                    var hikEx = ex as HikApi.HikException;
                    errorDetails = $@"<ul>
  <li>Error Code : {hikEx.ErrorCode}</li>
  <li>{hikEx.ErrorMessage}</li>
</ul>";
                }

                var msg = BuildBody(errorDetails, camera.ToHtmlTable(), ex.Message, ex.StackTrace);

                if (Settings != null)
                {
                    using var mail = new MailMessage
                    {
                        From = new MailAddress(Settings.UserName),
                    };
                    mail.To.Add(Settings.Receiver);
                    mail.Subject = $"{camera.Alias} error";
                    mail.Body = msg;
                    mail.IsBodyHtml = true;

#if DEBUG
                    logger.Info("Send");
                    logger.Info(msg);
#elif RELEASE
                    using var smtp = new SmtpClient(Settings.Server, Settings.Port)
                    {
                        Credentials = new NetworkCredential(Settings.UserName, Settings.Password),
                        EnableSsl = true,
                    };
                    smtp.Send(mail);
#endif
                }
            }
            catch (Exception e)
            {
                logger.Error(e, "Send failed");
            }
        }

        private static CameraConfig GetCameraConfig(Exception ex)
        {
            if (ex.Data.Contains("Camera") && ex.Data["Camera"] is CameraConfig)
            {
                return ex.Data["Camera"] as CameraConfig;
            }

            throw new ArgumentNullException("Camera config not found");
        }

        private static string BuildBody(string details, string cameraConfig, string message, string callStack)
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

{cameraConfig}

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