namespace Hik.Helpers.Email
{
    public interface IEmailHelper
    {
        void Send(string error, string className = null, string hikJobDetails = null);

        void Send(string subject, string body);
    }
}
