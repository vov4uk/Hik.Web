using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Hik.Client.Abstraction;
using Hik.Client.Abstraction.Services;
using Hik.DTO.Config;
using Hik.DTO.Contracts;
using Hik.Helpers.Abstraction;
using Serilog;
using MediaFiles = System.Collections.Generic.IReadOnlyCollection<Hik.DTO.Contracts.MediaFileDto>;

namespace Hik.Client.Service
{
    public class FtpUploaderService : RecurrentJobBase, IFtpUploaderService
    {
        private readonly IFilesHelper filesHelper;
        private readonly IUploaderClient ftp;
        private readonly ImageCodecInfo imageCodec;
        private readonly EncoderParameters encoderParameters;

        public FtpUploaderService(
            IDirectoryHelper directoryHelper,
            IFilesHelper filesHelper,
            IUploaderClient ftp,
            ILogger logger)
            : base(directoryHelper, logger)
        {
            this.filesHelper = filesHelper;
            this.ftp = ftp;
            this.imageCodec = ImageCodecInfo.GetImageEncoders().FirstOrDefault(c => c.FormatID == ImageFormat.Jpeg.Guid);
            this.encoderParameters = new EncoderParameters(1);
            encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, 25L);
        }

        protected override async Task<MediaFiles> RunAsync(BaseConfig config, DateTime from, DateTime to)
        {
            var fConfig = config as FtpUploaderConfig;
            var allFiles = this.directoryHelper.EnumerateFiles(fConfig.DestinationFolder, fConfig.AllowedFileExtentions).SkipLast(fConfig.SkipLast);

            var result = new List<MediaFileDto>();
            if (allFiles.Any())
            {
                foreach (var filePath in allFiles)
                {
                    try
                    {
                        if (IsPicture(filePath))
                        {
                            var tmp = filesHelper.GetTempFileName();
                            SaveJpg(filePath, tmp);
                            filesHelper.RenameFile(tmp, filePath);
                        }

                        filesHelper.ZipFile(filePath);
                        filesHelper.DeleteFile(filePath);
                    }
                    catch (IOException e)
                    {
                        logger.Error(e, "Failed to process file");
                    }
                    catch (InvalidDataException e)
                    {
                        logger.Error(e, "Invalid file");
                    }
                }

                var compressedFiles = this.directoryHelper.EnumerateFiles(fConfig.DestinationFolder, new[] { ".zip" });

                result.AddRange(compressedFiles.Select(x => new MediaFileDto
                {
                    Path = x,
                    Date = DateTime.Now,
                    Size = filesHelper.FileSize(x),
                    Name = filesHelper.GetFileName(x),
                }));

                await MoveToFtp(compressedFiles, fConfig.RemoteFolder);
            }

            return result;
        }

        private async Task MoveToFtp(IEnumerable<string> compressedFiles, string remoteFolder)
        {
            ftp.InitializeClient();
            if (ftp.Login())
            {
                await ftp.UploadFilesAsync(compressedFiles, remoteFolder);
                ftp.ForceExit();
                foreach (var filePath in compressedFiles)
                {
                    filesHelper.DeleteFile(filePath);
                }
            }
            else
            {
                ftp.ForceExit();
                throw new InvalidOperationException("Unable to login to FTP");
            }
        }

        private bool IsPicture(string filePath)
        {
            return filesHelper.GetExtension(filePath) == ".jpg";
        }

        private void SaveJpg(string source, string destination)
        {
            try
            {
                CompressImage(source, destination);
                filesHelper.DeleteFile(source);
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Error saving file {destination}");
            }
        }

        private void CompressImage(string source, string destination)
        {
            using (Bitmap bitmap = new Bitmap(source))
            {
                filesHelper.DeleteFile(destination);
                bitmap.Save(destination, this.imageCodec, this.encoderParameters);
            }
        }
    }
}
