namespace Gearbox.Core.Interfaces
{
  public interface IBrowser
  {
    string? Id { get; set; }
    string? Name { get; set; }
    string? Args { get; set; }
    string? Icon { get; set; }
    string? Path { get; set; }
  }
}
