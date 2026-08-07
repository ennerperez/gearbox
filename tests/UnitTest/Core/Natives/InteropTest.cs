using System.Runtime.Versioning;
using Gearbox.Core.Natives.Linux.Interop;
using Shouldly;
using Xunit;

namespace Gearbox.UnitTest.Core.Natives
{
    public class InteropTest
    {
#if LINUX

        [SupportedOSPlatform("linux")]
        [Fact(Skip = "Requires Linux desktop xdg-settings integration.")]
        public void ShouldGetSettingValue()
        {
            var currentValue = Xdg.GetSetting(Xdg.DEFAULT_WEB_BROWSER);
            currentValue.ShouldNotBeNullOrEmpty();
            currentValue.ShouldNotBe("org.gnome.Nautilus.desktop");
        }

        [SupportedOSPlatform("linux")]
        [Fact(Skip = "Requires Linux desktop xdotool integration.")]
        public void ShouldGetActiveWindowsName()
        {
            var activeWindowName = Xdo.GetActiveWindowName();
            activeWindowName.ShouldNotBeNullOrEmpty();
            activeWindowName.ShouldNotBe("N/A");
        }

#endif
    }
}
