namespace Job.Email
{
    public interface IEmailHelper
    {
        void Send(string error, string alias = null, string hikJobDetails = null);

        void Send(string subject, string body);
    }
}
