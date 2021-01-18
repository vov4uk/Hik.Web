using Hik.Api.Abstraction;
using Hik.Api.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hik.Api.Services
{
    public abstract class FileService<TRemoteFile>
        where TRemoteFile : IHikRemoteFile
    {
        public virtual async Task<IList<TRemoteFile>> FindFilesAsync(DateTime periodStart, DateTime periodEnd, Session session)
        {
            int findId = this.StartFind(session.UserId, periodStart, periodEnd, session.Device.ChannelNumber);

            IEnumerable<TRemoteFile> results = await this.GetFindResults(findId);

            this.FindClose(findId);
            return results.ToList();
        }

        protected abstract int StartFind(int userId, DateTime periodStart, DateTime periodEnd, int channel);

        internal abstract int FindNext(int findId, ref ISourceFile source);

        protected abstract bool FindClose(int findId);
        
        protected async Task<IEnumerable<TRemoteFile>> GetFindResults(int findId)
        {
            var results = new List<TRemoteFile>();
            ISourceFile sourceFile = default(ISourceFile);
            while (true)
            {
                int findStatus = FindNext(findId, ref sourceFile);

                if (findStatus == HikConst.NET_DVR_ISFINDING)
                {
                    await Task.Delay(500);
                }
                else if (findStatus == HikConst.NET_DVR_FILE_SUCCESS)
                {
                    results.Add((TRemoteFile)sourceFile.ToRemoteFile());
                }
                else
                {
                    break;
                }
            }

            return results;
        }
    }
}
