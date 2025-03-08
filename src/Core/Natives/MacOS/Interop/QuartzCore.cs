using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using MonoMac.CoreGraphics;
using MonoMac.Foundation;

namespace Gearbox.Core.Natives.MacOS.Interop
{
  
  [SupportedOSPlatform("macOS")]
  internal static class QuartzCore
  {

    internal class KCgWindowBounds
    {
      internal uint _height;
      internal uint _width;
      internal uint _x;
      internal uint _y;
      public override string ToString() => $"{{{_width}, {_height}, {_x}, {_y}}}";
    }
    
    internal class KCgWindow
    {
      internal uint _kCgWindowAlpha;
      internal KCgWindowBounds? _kCgWindowBounds;
      internal uint _kCgWindowIsOnscreen;
      internal int _kCgWindowLayer;
      internal uint _kCgWindowMemoryUsage;
      internal uint _kCgWindowNumber;
      internal string? _kCgWindowOwnerName;
      internal uint _kCgWindowOwnerPid;
      internal uint _kCgWindowSharingState;
      internal uint _kCgWindowStoreType;

      public override string ToString() => $"{_kCgWindowOwnerName} ({_kCgWindowOwnerPid})";

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
                iprop.SetValue(innerVal, uint.Parse(ival.Description));
              }
              else if (iprop.FieldType == typeof(int))
              {
                iprop.SetValue(innerVal, int.Parse(ival.Description));
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
            prop.SetValue(this, uint.Parse(value.Description));
          }
          else if (prop.FieldType == typeof(int))
          {
            prop.SetValue(this, int.Parse(value.Description));
          }
          
        }
      }
      
    }
    
    [DllImport(@"/System/Library/Frameworks/QuartzCore.framework/QuartzCore")]
    internal static extern IntPtr CGWindowListCopyWindowInfo(CGWindowListOption option, uint relativeToWindow);
  }
}
