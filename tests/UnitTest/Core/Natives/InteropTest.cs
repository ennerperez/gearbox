using System.Runtime.Versioning;
using Gearbox.Core.Natives.Linux.Interop;
using Shouldly;

namespace Gearbox.UnitTest.Core.Natives
{
    public class InteropTest
    {
#if LINUX

        [SupportedOSPlatform("linux")]
        [Fact]
        public void Should_Get_Setting_Value()
        {
            var currentValue = Xdg.GetSetting(Xdg.DEFAULT_WEB_BROWSER);
            currentValue.ShouldNotBeNullOrEmpty();
            currentValue.ShouldNotBe("org.gnome.Nautilus.desktop");
        }

        [SupportedOSPlatform("linux")]
        [Fact]
        public void Should_Get_Active_Windows_Name()
        {
            var activeWindowName = Xdo.GetActiveWindowName();
            activeWindowName.ShouldNotBeNullOrEmpty();
            activeWindowName.ShouldNotBe("N/A");
        }

#endif
    }
}
