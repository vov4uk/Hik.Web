﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hik.Client.Abstraction;
using Hik.Client.Events;
using Hik.DTO.Config;
using Hik.DTO.Contracts;
using NLog;

namespace Hik.Client.Service
{
    public class CleanUpService : RecurrentJobBase<MediaFileDTO>
    {
        private const double Gb = 1024.0 * 1024.0 * 1024.0;
        private readonly IDirectoryHelper directoryHelper;
        private readonly IFilesHelper filesHelper;

        public CleanUpService(IDirectoryHelper directoryHelper, IFilesHelper filesHelper)
        {
            this.directoryHelper = directoryHelper;
            this.filesHelper = filesHelper;
        }

        public override Task<IReadOnlyCollection<MediaFileDTO>> ExecuteAsync(BaseConfig config, DateTime from, DateTime to)
        {
            var cleanupConfig = config as CleanupConfig;
            var destination = cleanupConfig.DestinationFolder;
            this.logger.Info("Start CleanUpService");
            if (!this.directoryHelper.DirectoryExists(destination))
            {
                this.logger.Error($"Output doesn't exist: {destination}");
                return default;
            }

            var allFiles = this.directoryHelper.EnumerateFiles(destination).OrderByDescending(file => this.filesHelper.GetCreationDate(file));
            List<MediaFileDTO> deleteFilesResult = new List<MediaFileDTO>();

            this.logger.Info($"Destination: {destination}");
            this.logger.Info($"Found: {allFiles.Count()} files");

            double freePercentage = 0.0;
            int page = 0;
            int pageSize = cleanupConfig.BatchSize;
            do
            {
                var totalSpace = this.directoryHelper.GetTotalSpace(destination) / Gb;
                var freeSpace = this.directoryHelper.GetTotalFreeSpace(destination) / Gb;

                freePercentage = 100 * freeSpace / totalSpace;
                this.logger.Info($"Free Percentage: {freePercentage,2}");

                if (freePercentage < cleanupConfig.FreeSpacePercentage)
                {
                    var filesToDelete = allFiles.SkipLast(page * pageSize).TakeLast(pageSize).ToList();

                    try
                    {
                        var deletedFiles = this.DeleteFiles(filesToDelete, destination);
                        if (deletedFiles.Count <= 0)
                        {
                            break;
                        }

                        deleteFilesResult.AddRange(deletedFiles);
                        page++;
                    }
                    catch (Exception ex)
                    {
                        OnExceptionFired(new ExceptionEventArgs(ex), config);
                        break;
                    }
                }
                else
                {
                    break;
                }
            }
            while (true);

            directoryHelper.DeleteEmptyFolders(destination);

            return Task.FromResult(deleteFilesResult as IReadOnlyCollection<MediaFileDTO>);
        }

        private List<MediaFileDTO> DeleteFiles(List<string> filesToDelete, string basePath)
        {
            List<MediaFileDTO> result = new List<MediaFileDTO>();
            filesToDelete.ForEach(
                    file =>
                    {
                        this.logger.Debug($"Deleting: {file}");
                        if (filesHelper.FileExists(file))
                        {
                            var size = filesHelper.FileSize(file);
                            var date = filesHelper.GetCreationDate(file);
                            this.filesHelper.DeleteFile(file);
                            result.Add(new MediaFileDTO { Date = date, Name = file.Remove(0, basePath.Length), Size = size });
                        }
                    });
            return result;
        }
    }
}
