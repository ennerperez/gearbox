using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using MonoMac.CoreGraphics;
using MonoMac.Foundation;
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value

namespace Gearbox.Core.Natives.MacOS.Interop
{
    [SupportedOSPlatform("macOS")]
    internal static class QuartzCore
    {
        internal class KCgWindowBounds
        {
            internal uint Height;
            internal uint Width;
            internal uint X;
            internal uint Y;
            public override string ToString() => $"{{{Width}, {Height}, {X}, {Y}}}";
        }

        internal class KCgWindow
        {
            internal uint KCgWindowAlpha;
            internal KCgWindowBounds KCgWindowBounds;
            internal uint KCgWindowIsOnscreen;
            internal int KCgWindowLayer;
            internal uint KCgWindowMemoryUsage;
            internal uint KCgWindowNumber;
            internal string KCgWindowOwnerName;
            internal uint KCgWindowOwnerPid;
            internal uint KCgWindowSharingState;
            internal uint KCgWindowStoreType;

            public override string ToString() => $"{KCgWindowOwnerName} ({KCgWindowOwnerPid})";

            internal void Read(NSObject source)
            {
                var props = GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (var prop in props)
                {
                    var key = new NSString(prop.Name);
                    var value = source.ValueForKey(key);
                    if (prop.FieldType == typeof(KCgWindowBounds))
                    {
                        var innerProps = prop.FieldType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
                        var innerVal = new KCgWindowBounds();
                        foreach (var iprop in innerProps)
                        {
                            var ikey = new NSString(iprop.Name);
                            var ival = value.ValueForKey(ikey);
                            if (iprop.FieldType == typeof(string))
                            {
                                iprop.SetValue(innerVal, ival.Description);
                            }
                            else if (iprop.FieldType == typeof(uint))
                            {
                                iprop.SetValue(innerVal, uint.Parse(ival.Description, CultureInfo.CurrentCulture));
                            }
                            else if (iprop.FieldType == typeof(int))
                            {
                                iprop.SetValue(innerVal, int.Parse(ival.Description, CultureInfo.CurrentCulture));
                            }
                        }

                        prop.SetValue(this, innerVal);
                    }
                    else if (prop.FieldType == typeof(string))
                    {
                        prop.SetValue(this, value.Description);
                    }
                    else if (prop.FieldType == typeof(uint))
                    {
                        prop.SetValue(this, uint.Parse(value.Description, CultureInfo.CurrentCulture));
                    }
                    else if (prop.FieldType == typeof(int))
                    {
                        prop.SetValue(this, int.Parse(value.Description, CultureInfo.CurrentCulture));
                    }
                }
            }
        }

        [DllImport(@"/System/Library/Frameworks/QuartzCore.framework/QuartzCore")]
        internal static extern IntPtr CGWindowListCopyWindowInfo(CGWindowListOption option, uint relativeToWindow);
    }
}
