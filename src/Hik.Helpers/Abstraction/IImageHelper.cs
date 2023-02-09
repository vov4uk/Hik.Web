using System;

namespace Hik.Helpers.Abstraction
{
    public interface IImageHelper
    {
        void SetDate(string path, string newPath, DateTime date);

        byte[] GetThumbnail(string path, int width, int height);

        string GetDescriptionData(string path);
    }
}
