namespace Nemui.Infrastructure.Data.Seeds;

public interface ISeeder
{
    Task SeedAsync();
    int Order { get; }
    string Name { get; }
}