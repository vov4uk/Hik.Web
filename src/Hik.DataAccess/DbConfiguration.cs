using Hik.DataAccess.Abstractions;

namespace Hik.DataAccess
{
    public class DbConfiguration : IDbConfiguration
    {
        public string ConnectionString
        {
            get;
            set;
        }

        public int? CommandTimeout
        {
            get;
            set;
        }

        public DbConfiguration()
        {
        }
    }
}
