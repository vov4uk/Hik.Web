using System;

namespace Job.Email
{
    public interface IEmailHelper
    {
        void Send(Exception ex, string alias = null, string hikJobDetails = null);

        void Send(string subject, string body);
    }
}
