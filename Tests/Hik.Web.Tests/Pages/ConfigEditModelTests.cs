using AutoFixture.Xunit2;
using Hik.Quartz.Contracts;
using Hik.Web.Commands.Config;
using Hik.Web.Pages;
using Hik.Web.Queries.QuartzJobConfig;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Hik.Web.Tests.Pages
{
    public class ConfigEditModelTests : ModelTestsBase
    {
        private readonly CronConfigDto CronConfig = new() { JobName = name, Json = "", Path = "C:\\" };
        private const string JsonKey = "Dto.ConfigDTO.Json";

        [Theory]
        [AutoData]
        public async Task OnGetAsync_ReturnDto(string group, string name)
        {
            this._mediator.Setup(x => x.Send(It.Is<QuartzJobConfigQuery>(y => y.Group == group && y.Name == name), default(CancellationToken)))
                .ReturnsAsync(new QuartzJobConfigDto { Config = CronConfig});
            var sut = new ConfigEditModel(this._mediator.Object);
            await sut.OnGetAsync(name, group);

            Assert.NotNull(sut.Dto);
            Assert.NotNull(sut.Dto.Config.Path);
            Assert.NotNull(sut.Dto.Config.Json);
            Assert.NotNull(sut.Dto.Config.JobName);
        }

        [Fact]
        public async Task OnPostAsync_InvalidModel_NothingSaved()
        {
            var sut = new ConfigEditModel(this._mediator.Object);
            sut.ModelState.TryAddModelException("Dto", new System.Exception());
            var result = await sut.OnPostAsync();

            Assert.IsType<RedirectToPageResult>(result);
            var page = (RedirectToPageResult)result;
            Assert.Equal("./Scheduler", page.PageName);
        }

        [Fact]
        public async Task OnPostAsync_Model_StringSaved()
        {
            this._mediator.Setup(x => x.Send(It.Is<QuartzJobConfigQuery>(y => y.Group == group && y.Name == name), default(CancellationToken)))
                .ReturnsAsync(new QuartzJobConfigDto { Config = CronConfig });

            this._mediator.Setup(x => x.Send(It.Is<UpdateQuartzJobConfigCommand>(y => y.Json == "Abra kadabra"), default(CancellationToken)))
                .ReturnsAsync(MediatR.Unit.Value);
            var sut = new ConfigEditModel(this._mediator.Object);
            await sut.OnGetAsync(name, group);
            sut.ModelState.SetModelValue(JsonKey, "Abra kadabra", "");
            sut.ModelState[JsonKey].ValidationState = Microsoft.AspNetCore.Mvc.ModelBinding.ModelValidationState.Valid;
            var result = await sut.OnPostAsync();

            Assert.IsType<RedirectToPageResult>(result);
            var page = (RedirectToPageResult)result;
            Assert.Equal("./Scheduler", page.PageName);
        }

        [Fact]
        public async Task OnPostAsync_Model_StringArraySaved()
        {
            this._mediator.Setup(x => x.Send(It.Is<QuartzJobConfigQuery>(y => y.Group == group && y.Name == name), default(CancellationToken)))
                .ReturnsAsync(new QuartzJobConfigDto { Config = CronConfig });

            this._mediator.Setup(x => x.Send(It.Is<UpdateQuartzJobConfigCommand>(y => y.Json == "Abra kadabra"), default(CancellationToken)))
                .ReturnsAsync(MediatR.Unit.Value);
            var sut = new ConfigEditModel(this._mediator.Object);
            await sut.OnGetAsync(name, group);
            sut.ModelState.SetModelValue(JsonKey, new string[] {"Abra", "Abra kadabra"}, "");
            sut.ModelState[JsonKey].ValidationState = Microsoft.AspNetCore.Mvc.ModelBinding.ModelValidationState.Valid;
            var result = await sut.OnPostAsync();

            Assert.IsType<RedirectToPageResult>(result);
            var page = (RedirectToPageResult)result;
            Assert.Equal("./Scheduler", page.PageName);
        }
    }
}
