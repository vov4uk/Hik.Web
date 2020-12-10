﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using HikConsole.DataAccess.Data;
using HikConsole.DTO;
using HikConsole.Infrastructure;
using HikConsole.Scheduler;
using Job.Email;

namespace Job.Impl
{
    public class HikDownloaderJob : JobProcessBase
    {
        public HikDownloaderJob(string description, string path, string connectionString, Guid activityId) : base(description, path, connectionString, activityId)
        {

        }

        public override JobType JobType => JobType.HikDownloader;


        public async override Task<JobResult> Run()
        {
            Dictionary<string, DateTime?> lastSync;

            using (var unitOfWork = this.UnitOfWorkFactory.CreateUnitOfWork())
            {
                var cameraRepo = unitOfWork.GetRepository<Camera>();
                var camerasList = await cameraRepo.GetAllAsync();
                lastSync = camerasList.ToDictionary(k => k.Alias, v => v.LastSync);
            }

            var downloader = AppBootstrapper.Container.Resolve<HikDownloader>();
            downloader.SetLastSyncDates(lastSync);
            downloader.ExceptionFired += Downloader_ExceptionFired;
            downloader.VideoDownloaded += Downloader_VideoDownloaded;
            return await downloader.ExecuteAsync(this.ConfigPath);
        }

        private async void Downloader_VideoDownloaded(object sender, HikConsole.Events.VideoEventArgs e)
        {
            Logger.Info("Save Video to DB...");
            var jobResultSaver = new JobService(this.UnitOfWorkFactory, JobInstance);
            await jobResultSaver.SaveVideoAsync(e.Video, e.Camera.Alias);
            Logger.Info("Save Video to DB. Done");
        }

        private void Downloader_ExceptionFired(object sender, HikConsole.Events.ExceptionEventArgs e)
        {
            base.Logger.Error(e.Exception, e.Exception.Message);
            EmailHelper.Send(e.Exception);
            Console.WriteLine(e.ToString());
        }
    }
}
