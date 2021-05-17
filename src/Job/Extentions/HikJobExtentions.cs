using System.Text;
using Hik.DataAccess.Data;
using Hik.DTO.Config;

namespace Job.Extentions
{
    public static class HikJobExtentions
    {
        public static string ToHtmlTable(this HikJob job, BaseConfig config)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<table>");
            sb.AppendLine(config.ToHtmlTable());
            sb.AppendLine(GetHtmlRow(nameof(job.Started), $"{job.Started} ({Hik.Client.Helpers.Utils.GetRelativeTime(job.Started)})"));
            sb.AppendLine(GetHtmlRow(nameof(job.Finished), $"{job.Finished} ({Hik.Client.Helpers.Utils.GetRelativeTime(job.Finished)})"));
            sb.AppendLine(GetHtmlRow(nameof(job.PeriodStart), $"{job.PeriodStart} ({Hik.Client.Helpers.Utils.GetRelativeTime(job.PeriodStart)})"));
            sb.AppendLine(GetHtmlRow(nameof(job.PeriodEnd), $"{job.PeriodEnd} ({Hik.Client.Helpers.Utils.GetRelativeTime(job.PeriodEnd)})"));
            sb.AppendLine(GetHtmlRow(nameof(job.FilesCount), job.DownloadedFiles.Count.ToString()));
            sb.AppendLine(GetHtmlRow("Latest", job.DownloadedFiles?.FindLast(x => true)?.MediaFile?.Date.ToString()));
            sb.AppendLine("</table>");

            return sb.ToString();
        }

        private static string GetHtmlRow(string field, string value)
        {
            return $"<tr><td>{field}</td><td>{value}</td></tr>";
        }
    }
}
