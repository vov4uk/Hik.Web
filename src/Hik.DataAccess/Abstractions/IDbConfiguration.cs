namespace Hik.DataAccess.Abstractions
{
    public interface IDbConfiguration
    {
        string ConnectionString
        {
            get;
            set;
        }

        int? CommandTimeout
        {
            get;
            set;
        }
    }
}
