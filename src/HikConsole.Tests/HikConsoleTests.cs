using System;
using System.Collections.Generic;
using AutoFixture;
using HikConsole;
using HikConsole.Abstraction;
using HikConsole.Data;
using Moq;
using NUnit.Framework;

namespace HikConsoleTests
{    
    public class HikConsoleTests
    {
        delegate void LoginDelegate(string sDVRIP, int wDVRPort, string sUserName, string sPassword, ref DeviceInfo deviceInfo);

        [Test]
        public void Init_CallInit_ClientInitialized()
        {
            var sdkMock = new Mock<ISDKWrapper>(MockBehavior.Strict);
            sdkMock.Setup(x => x.Initialize()).Returns(true);
            sdkMock.Setup(x => x.SetupSDKLogs(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(true);

            var fixture = new Fixture();
            var configMock = fixture.Create<AppConfig>();

            var client = new HikConsole.HikConsole(configMock, sdkMock.Object);
            client.Init();

            sdkMock.Verify(x => x.Initialize(), Times.Once);
            sdkMock.Verify(x => x.SetupSDKLogs(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Once);
        }

        [Test]
        public void Login_CallLogin_LoginSucessfully()
        {
            var sdkMock = new Mock<ISDKWrapper>(MockBehavior.Strict);

            DeviceInfo inDevice = default(DeviceInfo);
            DeviceInfo outDevice = new DeviceInfo();

            sdkMock.Setup(x => x.Login(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), ref inDevice))
                .Callback(new LoginDelegate((string ip, int port, string user, string pass, ref DeviceInfo dev) =>
                {
                    dev = outDevice;
                }))
                .Returns(1);

            var client = new HikConsole.HikConsole(new AppConfig(), sdkMock.Object);
            bool res = client.Login();

            sdkMock.Verify(x => x.Login(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), ref outDevice), Times.Once);
            Assert.IsTrue(res);
        }

        [Test]
        public void Login_CallLoginTwice_LoginOnce()
        {
            var sdkMock = new Mock<ISDKWrapper>(MockBehavior.Strict);

            DeviceInfo inDevice = default(DeviceInfo);
            var outDevice = new DeviceInfo();

            sdkMock.Setup(x => x.Login(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), ref inDevice))
                .Callback(new LoginDelegate((string ip, int port, string user, string pass, ref DeviceInfo dev) =>
                {
                    dev = outDevice;
                }))
                .Returns(1);

            var client = new HikConsole.HikConsole(new AppConfig(), sdkMock.Object);
            var first = client.Login();
            var second = client.Login();

            Assert.IsTrue(first);
            Assert.IsFalse(second);
            sdkMock.Verify(x => x.Login(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), ref outDevice), Times.Once);
        }

        [Test]
        public void Logout_CallLogin_LogoutSuccess()
        {
            var sdkMock = new Mock<ISDKWrapper>(MockBehavior.Strict);

            DeviceInfo inDevice = default(DeviceInfo);
            var outDevice = new DeviceInfo();

            var userId = 1;
            sdkMock.Setup(x => x.Login(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), ref inDevice))
                .Callback(new LoginDelegate((string ip, int port, string user, string pass, ref DeviceInfo dev) =>
                {
                    dev = outDevice;
                }))
                .Returns(userId);
            sdkMock.Setup(x => x.Logout(userId));

            var client = new HikConsole.HikConsole(new AppConfig(), sdkMock.Object);
            var first = client.Login();
            client.Logout();

            Assert.IsTrue(first);
            sdkMock.Verify(x => x.Login(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), ref outDevice), Times.Once);
            sdkMock.Verify(x => x.Logout(userId), Times.Once);
        }

        [Test]
        public void Logout_DontCallLogin_LogoutNotCall()
        {
            var sdkMock = new Mock<ISDKWrapper>(MockBehavior.Strict);            
            var client = new HikConsole.HikConsole(new AppConfig(), sdkMock.Object);

            client.Logout();

            sdkMock.Verify(x => x.Logout(It.IsAny<int>()), Times.Never);
        }

        [Test]
        public void Find_CallLogin_CallFindWithCorrectParameters()
        {
            var sdkMock = new Mock<ISDKWrapper>(MockBehavior.Strict);

            DateTime start = new DateTime();
            DateTime end = start.AddSeconds(1);
            DeviceInfo inDevice = default(DeviceInfo);
            var outDevice = new DeviceInfo() {StartChannel = 1};

            var userId = 1;
            sdkMock.Setup(x => x.Login(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), ref inDevice))
                .Callback(new LoginDelegate((string ip, int port, string user, string pass, ref DeviceInfo dev) =>
                {
                    dev = outDevice;
                }))
                .Returns(userId);

            sdkMock.Setup(x => x.Find(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(new List<FindResult>());

            var client = new HikConsole.HikConsole(new AppConfig(), sdkMock.Object);
            var first = client.Login();

            var find = client.Find(start, end);

            sdkMock.Verify(x => x.Find(start, end, userId, outDevice.StartChannel), Times.Once);
        }

        [Test]
        public void Find_CallFindWithInvalidParametersa_ThrowsException()
        {
            var sdkMock = new Mock<ISDKWrapper>(MockBehavior.Strict);
            var client = new HikConsole.HikConsole(new AppConfig(), sdkMock.Object);

            Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                var date = new DateTime(1970, 1, 1);
                var res = await client.Find(date, date);
            });
        }
    }
}
