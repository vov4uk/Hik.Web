using System;

namespace Hik.Client.Abstraction
{
    public interface IClientBase : IDisposable
    {
        void InitializeClient();

        bool Login();

        void ForceExit();
    }
}