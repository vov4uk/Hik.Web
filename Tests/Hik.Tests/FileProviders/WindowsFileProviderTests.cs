using Hik.Client.Abstraction;
using Hik.Client.FileProviders;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Hik.Client.Tests.FileProviders
{
    public class WindowsFileProviderTests
    {
        private readonly Mock<IFilesHelper> filesMock;
        private readonly Mock<IDirectoryHelper> dirMock;
        private readonly Mock<IVideoHelper> videoMock;

        private const string ext = ".*";
        private const string folder1 = "C:\\Folder1";
        private const string subFolder1 = "C:\\Folder1\\2022-01\\03\\13";
        private const string subFolder2 = "C:\\Folder1\\2022-01\\03\\14";
        private const string folder2 = "C:\\Folder2";
        private const string subFolder21 = "C:\\Folder2\\2022-01\\03\\13";
        private const string subFolder22 = "C:\\Folder2\\2022-01\\03\\14";

        public WindowsFileProviderTests()
        {
            this.videoMock = new (MockBehavior.Strict);
            this.dirMock = new (MockBehavior.Strict);
            this.filesMock = new (MockBehavior.Strict);
        }

        [Fact]
        public void InitializeClient_CallInitializeClient_ClientInitialized()
        {
            dirMock.Setup(x => x.EnumerateAllDirectories(folder1))
                .Returns(new List<string>() { subFolder1 })
                .Verifiable();
            dirMock.Setup(x => x.EnumerateAllDirectories(folder2))
                .Returns(new List<string>() { subFolder21, subFolder22 })
                .Verifiable();

            var fileProvider = GetFileProvider();
            fileProvider.Initialize(new string[] { folder1, folder2 });

            dirMock.VerifyAll();
            Assert.True(fileProvider.IsInitialized);
        }

        [Fact]
        public void GetNextBatch_NotInitialized_ReturnsEmptyList()
        {
            var fileProvider = GetFileProvider();
            var actual = fileProvider.GetNextBatch(".*");

            Assert.Empty(actual);
        }

        [Fact]
        public void GetNextBatch_Initialized_ReturnsFilesList()
        {
            dirMock.Setup(x => x.EnumerateAllDirectories(folder1))
                .Returns(new List<string>() { subFolder1, subFolder2 })
                .Verifiable();
            dirMock.Setup(x => x.EnumerateAllDirectories(folder2))
                .Returns(new List<string>() { subFolder21 })
                .Verifiable();
            dirMock.Setup(x => x.EnumerateFiles(subFolder1, It.IsAny<string[]>()))
                .Returns(new List<string>
                {
                    Path.Combine(subFolder1, "20220103_135655.jpg"),
                    Path.Combine(subFolder1, "20220103_135656.jpg"),
                    Path.Combine(subFolder1, "20220103_135657.jpg"),
                    Path.Combine(subFolder1, "20220103_135658.jpg"),
                    Path.Combine(subFolder1, "20220103_135659.jpg"),
                });
            dirMock.Setup(x => x.EnumerateFiles(subFolder2, It.IsAny<string[]>()))
                .Returns(new List<string>
                {
                    Path.Combine(subFolder2, "20220103_135655.jpg"),
                    Path.Combine(subFolder2, "20220103_135659.jpg"),
                });
            dirMock.Setup(x => x.EnumerateFiles(subFolder21, It.IsAny<string[]>()))
                .Returns(new List<string>
                {
                    Path.Combine(subFolder21, "20220103_135654.jpg"),
                    Path.Combine(subFolder21, "20220103_135655.jpg"),
                    Path.Combine(subFolder21, "20220103_135656.jpg"),
                });
            dirMock.Setup(x => x.EnumerateFiles(folder1, It.IsAny<string[]>()))
                .Returns(new List<string>());
            dirMock.Setup(x => x.EnumerateFiles(folder2, It.IsAny<string[]>()))
                .Returns(new List<string>());

            var fileProvider = GetFileProvider();
            fileProvider.Initialize(new string[] { folder1, folder2 });

            var firstBatch = fileProvider.GetNextBatch(ext, 5).ToList();
            var secondBatch = fileProvider.GetNextBatch(ext, 5).ToList();

            dirMock.VerifyAll();
            Assert.Equal(8, firstBatch.Count);
            Assert.True(firstBatch.TrueForAll(x => x.Date == new DateTime(2022, 01, 03, 13, 0, 0)));
            Assert.Equal(2, secondBatch.Count);
            Assert.True(secondBatch.TrueForAll(x => x.Date == new DateTime(2022, 01, 03, 14, 0, 0)));
        }

        [Fact]
        public void GetNextBatch_NoExtention_ReturnsAllFiles()
        {
            dirMock.Setup(x => x.EnumerateAllDirectories(folder1))
                .Returns(new List<string>() { subFolder1 })
                .Verifiable();

            dirMock.Setup(x => x.EnumerateFiles(subFolder1))
                .Returns(new List<string>
                {
                    Path.Combine(subFolder1, "20220103_135655.jpg"),
                    Path.Combine(subFolder1, "20220103_135656.jpg"),
                    Path.Combine(subFolder1, "20220103_135657.jpg"),
                    Path.Combine(subFolder1, "20220103_135658.jpg"),
                    Path.Combine(subFolder1, "20220103_135659.jpg"),
                });

            dirMock.Setup(x => x.EnumerateFiles(folder1))
                .Returns(new List<string>());

            var fileProvider = GetFileProvider();
            fileProvider.Initialize(new string[] { folder1 });

            var firstBatch = fileProvider.GetNextBatch(null, 5).ToList();

            dirMock.VerifyAll();
            Assert.Equal(5, firstBatch.Count);
            Assert.True(firstBatch.TrueForAll(x => x.Date == new DateTime(2022, 01, 03, 13, 0, 0)));
        }

        [Fact]
        public async Task GetOldestFilesBatch_NotInitialized_ReturnsEmptyList()
        {
            var fileProvider = GetFileProvider();
            var actual = await fileProvider.GetOldestFilesBatch();

            Assert.Empty(actual);
        }

        [Fact]
        public async Task GetOldestFilesBatch_Initialized_ReturnsFilesList()
        {
            dirMock.Setup(x => x.EnumerateAllDirectories(folder1))
                .Returns(new List<string>() { subFolder1, subFolder2 })
                .Verifiable();

            dirMock.Setup(x => x.EnumerateFiles(subFolder1, It.IsAny<string[]>()))
                .Returns(new List<string>
                {
                    Path.Combine(subFolder1, "20220103_135655.jpg"),
                    Path.Combine(subFolder1, "20220103_135656.jpg"),
                    Path.Combine(subFolder1, "20220103_135657.jpg"),
                    Path.Combine(subFolder1, "20220103_135658.jpg"),
                    Path.Combine(subFolder1, "20220103_135659.jpg"),
                });
            dirMock.Setup(x => x.EnumerateFiles(subFolder2, It.IsAny<string[]>()))
                .Returns(new List<string>
                {
                    Path.Combine(subFolder2, "20220103_145655.jpg"),
                    Path.Combine(subFolder2, "20220103_145659.jpg"),
                });

            filesMock.Setup(x => x.FileSize(It.IsAny<string>()))
                .Returns(1024);
            filesMock.Setup(x => x.GetFileName(It.IsAny<string>()))
                .Returns(string.Empty);
            videoMock.Setup(x => x.GetDuration(It.IsAny<string>()))
                .ReturnsAsync(60);

            var fileProvider = GetFileProvider();
            fileProvider.Initialize(new string[] { folder1 });

            var firstBatch = await fileProvider.GetOldestFilesBatch(true);
            var secondBatch = await fileProvider.GetOldestFilesBatch(false);

            dirMock.VerifyAll();
            Assert.Equal(5, firstBatch.Count);
            Assert.True(firstBatch.ToList()
                .TrueForAll(x => x.Date == new DateTime(2022, 01, 03, 13, 0, 0) && x.Duration == 60 && x.Size == 1024));
            Assert.Equal(2, secondBatch.Count);
            Assert.True(secondBatch.ToList()
                .TrueForAll(x => x.Date == new DateTime(2022, 01, 03, 14, 0, 0) && x.Duration == 0 && x.Size == 1024));
        }

        [Fact]
        public void GetFilesOlderThan_NotInitialized_ReturnsEmptyList()
        {
            var fileProvider = GetFileProvider();
            var actual = fileProvider.GetFilesOlderThan(ext, DateTime.Today);

            Assert.Empty(actual);
        }

        [Fact]
        public void GetFilesOlderThan_InitializedNoFiles_ReturnsEmptyList()
        {
            dirMock.Setup(x => x.EnumerateAllDirectories(folder1))
                .Returns(new List<string>())
                .Verifiable();
            dirMock.Setup(x => x.EnumerateFiles(folder1, It.IsAny<string[]>()))
                .Returns(new List<string>());

            var fileProvider = GetFileProvider();
            fileProvider.Initialize(new string[] {folder1});
            var actual = fileProvider.GetFilesOlderThan(ext, DateTime.Today);

            Assert.Empty(actual);
        }

        [Fact]
        public void GetFilesOlderThan_Initialized_ReturnsFilesList()
        {
            dirMock.Setup(x => x.EnumerateAllDirectories(folder1))
                .Returns(new List<string>() { subFolder1, subFolder2 })
                .Verifiable();

            dirMock.Setup(x => x.EnumerateFiles(subFolder1, It.IsAny<string[]>()))
                .Returns(new List<string>
                {
                    Path.Combine(subFolder1, "20220103_135655.jpg"),
                    Path.Combine(subFolder1, "20220103_135656.jpg"),
                    Path.Combine(subFolder1, "20220103_135657.jpg"),
                    Path.Combine(subFolder1, "20220103_135658.jpg"),
                    Path.Combine(subFolder1, "20220103_135659.jpg"),
                });

            var fileProvider = GetFileProvider();
            fileProvider.Initialize(new string[] { folder1 });

            var firstBatch = fileProvider.GetFilesOlderThan(ext, new DateTime(2022, 01, 03, 13, 0, 0));

            dirMock.VerifyAll();
            Assert.Equal(5, firstBatch.Count);
            Assert.True(firstBatch.ToList()
                .TrueForAll(x => x.Date == new DateTime(2022, 01, 03, 13, 0, 0)));
            dirMock.Verify(x => x.EnumerateFiles(subFolder2, It.IsAny<string[]>()), Times.Never);
        }

        private WindowsFileProvider GetFileProvider()
            => new WindowsFileProvider(filesMock.Object, dirMock.Object, videoMock.Object);
    }
}
