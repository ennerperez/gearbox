namespace Gearbox.Core.Interfaces
{
    public interface IBrowser
    {
        string? Id { get; set; }
        string? Name { get; set; }
        string? Command { get; set; }
        string? Args { get; set; }
        string? Icon { get; set; }
        bool IsInstalled { get; }
        public string WorkingDirectory { get; }
        string? Path { get; set; }
    }
}
