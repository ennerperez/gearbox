using System;
using System.IO;
using Gearbox.Core.Interfaces;

namespace Gearbox.Core.Models
{
    public class Browser : IBrowser
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Command { get; set; }
        public string Args { get; set; }
        public string Icon { get; set; }
        public bool IsInstalled => Path != null && File.Exists(Environment.ExpandEnvironmentVariables(Path));
        public string WorkingDirectory => (Path != null ? new FileInfo(Environment.ExpandEnvironmentVariables(Path)).Directory?.FullName : string.Empty) ?? string.Empty;
        public string Path { get; set; }

        public override string ToString() => string.Join(" ", new[] { System.IO.Path.GetFileName(Path), Command });
    }
}
