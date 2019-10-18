using AutoFixture;
using HikConsole;
using HikConsole.Abstraction;
using HikConsole.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;

namespace HikConsoleTests
{
    [TestClass]
    public class HikConsoleTest
    {
        delegate void LoginDelegate(string sDVRIP, int wDVRPort, string sUserName, string sPassword, ref DeviceInfo deviceInfo);

        [TestMethod]
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

        [TestMethod]
        public void Login_CallLogin_LoginSucessfully()
        {
            var sdkMock = new Mock<ISDKWrapper>(MockBehavior.Strict);

            DeviceInfo inDevice = default;
            var outDevice = new DeviceInfo();

            sdkMock.Setup(x => x.Login(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), ref inDevice))
                .Callback(new LoginDelegate((string ip, int port, string user, string pass, ref DeviceInfo dev) =>
                {
                    dev = outDevice;
                }))
                .Returns(1);

            var client = new HikConsole.HikConsole(new AppConfig(), sdkMock.Object);
            var res = client.Login();

            sdkMock.Verify(x => x.Login(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), ref outDevice), Times.Once);
            Assert.IsTrue(res);
        }

        [TestMethod]
        public void Login_CallLoginTwice_LoginOnce()
        {
            var sdkMock = new Mock<ISDKWrapper>(MockBehavior.Strict);

            DeviceInfo inDevice = default;
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
    }
}
