using System;

namespace Gearbox.Core
{
  public static class Metadata
  {
    public static string? Name { get; set; }
    public static string? Assembly { get; set; }
    public static string? Description { get; set; }
    public static string? DisplayVersion { get; set; }
    public static Version? Version { get; set; }
    public static string? Commit { get; set; }
    public static string? Tag { get; set; }
    public static string? Environment { get; set; }
  }
}
