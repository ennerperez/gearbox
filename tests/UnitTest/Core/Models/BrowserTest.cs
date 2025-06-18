using Gearbox.Core.Models;
using Shouldly;

namespace Gearbox.UnitTest.Core.Models
{
    public class BrowserTest
    {
        [Fact]
        public void Should_Create_A_New_Browser()
        {
            var model = new Browser()
            {
                Args = "--new-window",
                Icon = "https://example.com/icon.png",
                Id = "browser1",
                Name = "Test Browser",
                Path = "/usr/bin/test-browser",
            };
            model.Id.ShouldNotBeEmpty();
            model.Name.ShouldNotBeEmpty();
            model.Args.ShouldNotBeEmpty();
            model.Icon.ShouldNotBeEmpty();
            //model.IsInstalled.ShouldBeTrue();
            model.ShouldNotBeNull();
        }

    }
}
