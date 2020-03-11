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
        public void SendEmail(EmailConfig settings, Exception ex)
        {
            try
            {
                var camera = this.GetCameraConfig(ex);
                string errorDetails = string.Empty;

                if (ex is HikApi.HikException)
                {
                    var hikEx = ex as HikApi.HikException;
                    errorDetails = $@"<ul>
  <li>Error Code : {hikEx.ErrorCode}</li>
  <li>{hikEx.ErrorMessage}</li>  
</ul>";
                }

                var msg = this.BuildBody(errorDetails, camera.ToHtmlTable(), ex.Message, ex.StackTrace);

                if (settings != null)
                {
                    using var mail = new MailMessage
                    {
                        From = new MailAddress(settings.UserName),
                    };
                    mail.To.Add(settings.Receiver);
                    mail.Subject = $"{camera.Alias} error";
                    mail.Body = msg;
                    mail.IsBodyHtml = true;

                    using var smtp = new SmtpClient(settings.Server, settings.Port)
                    {
                        Credentials = new NetworkCredential(settings.UserName, settings.Password),
                        EnableSsl = true,
                    };

#if DEBUG
                    Logger.Instance.Info("SendEmail");
                    Logger.Instance.Info(msg);
                    smtp.Dispose();
#elif RELEASE

                    smtp.Send(mail);
#endif
                }
            }
            catch (Exception e)
            {
                Logger.Instance.Error("SendEmail failed", e);
            }
        }

        private CameraConfig GetCameraConfig(Exception ex)
        {
            if (ex.Data.Contains("Camera") && ex.Data["Camera"] is CameraConfig)
            {
                return ex.Data["Camera"] as CameraConfig;
            }

            throw new ArgumentNullException("Camera config not found");
        }

        private string BuildBody(string details, string cameraConfig, string message, string callStack)
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
    }
}