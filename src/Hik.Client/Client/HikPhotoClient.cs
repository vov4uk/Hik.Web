using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Hik.Api.Abstraction;
using Hik.Client.Abstraction;
using Hik.Client.Helpers;
using Hik.DTO.Config;
using Hik.DTO.Contracts;

namespace Hik.Client
{
    public class HikPhotoClient : HikBaseClient, IClient
    {
        public HikPhotoClient(CameraConfig config, IHikApi hikApi, IFilesHelper filesHelper, IMapper mapper)
            : base(config, hikApi, filesHelper, mapper)
        {
        }

        public Task<bool> DownloadFileAsync(MediaFileDTO file, CancellationToken token)
        {
            if (!IsDownloading)
            {
                string targetFilePath = GetPathSafety(file);

                if (!filesHelper.FileExists(targetFilePath))
                {
                    string tempFile = ToFileNameString(file);
                    hikApi.PhotoService.DownloadFile(session.UserId, file.Name, file.Size, tempFile);

                    SetDate(tempFile, targetFilePath, file.Date);
                    filesHelper.DeleteFile(tempFile);

                    return Task.FromResult(true);
                }

                return Task.FromResult(false);
            }
            else
            {
                logger.Warn("HikClient.PhotoDownload : Downloading, please stop firstly!");
                return Task.FromResult(false);
            }
        }

        public async Task<IList<MediaFileDTO>> GetFilesListAsync(DateTime periodStart, DateTime periodEnd)
        {
            ValidateDateParameters(periodStart, periodEnd);

            logger.Info($"Get photos from {periodStart} to {periodEnd}");

            var remoteFiles = await hikApi.PhotoService.FindFilesAsync(periodStart, periodEnd, session);

            return Mapper.Map<IList<MediaFileDTO>>(remoteFiles);
        }

        protected override void StopDownload()
        {
        }

        protected override string ToFileNameString(MediaFileDTO file)
        {
            return file.ToPhotoFileNameString();
        }

        protected override string ToDirectoryNameString(MediaFileDTO file)
        {
            return file.Date.ToPhotoDirectoryNameString();
        }

        private void SetDate(string path, string newPath, DateTime date)
        {
            using Image image = Image.FromFile(path);
            var newItem = (PropertyItem)FormatterServices.GetUninitializedObject(typeof(PropertyItem));
            newItem.Value = Encoding.ASCII.GetBytes(date.ToString("yyyy':'MM':'dd' 'HH':'mm':'ss"));
            newItem.Type = 2;
            newItem.Id = 306;
            image.SetPropertyItem(newItem);
            image.Save(newPath, image.RawFormat);
        }
    }
}
