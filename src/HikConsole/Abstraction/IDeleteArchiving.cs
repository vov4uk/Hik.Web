using System;
using System.Threading.Tasks;

namespace HikConsole.Abstraction
{
    public interface IDeleteArchiving
    {
        Task Archive(string destination, TimeSpan time);
    }
}
