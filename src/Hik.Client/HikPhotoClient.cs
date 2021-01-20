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
using Hik.Api.Data;
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

        public Task<bool> DownloadFileAsync(FileDTO file, CancellationToken token)
        {
            if (!IsDownloading)
            {
                string destinationFilePath = GetPathSafety(file);

                if (!CheckLocalPhotoExist(destinationFilePath, file.Size))
                {
                    string tempFile = ToFileNameString(file);
                    hikApi.PhotoService.DownloadFile(session.UserId, file.Name, file.Size, tempFile);

                    SetDate(tempFile, destinationFilePath, file.Date);
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

        public async Task<IList<FileDTO>> GetFilesListAsync(DateTime periodStart, DateTime periodEnd)
        {
            ValidateDateParameters(periodStart, periodEnd);

            logger.Info($"Get photos from {periodStart} to {periodEnd}");

            var remoteFiles = await hikApi.PhotoService.FindFilesAsync(periodStart, periodEnd, session);

            return Mapper.Map<IList<FileDTO>>(remoteFiles);
        }

        protected override void StopDownload()
        {
        }

        protected override string ToFileNameString(FileDTO file)
        {
            return file.ToPhotoFileNameString();
        }

        protected override string ToDirectoryNameString(FileDTO file)
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

        private bool CheckLocalPhotoExist(string path, long size)
        {
            // Downloaded video file is bigger than remote file
            // 56 bytes for 2MP camera
            // 70 bytes for 4MP camera
            // This const was taken on runtime
            long fileSize = filesHelper.FileSize(path);
            return size + 70 == fileSize || size + 56 == fileSize;
        }
    }
}
