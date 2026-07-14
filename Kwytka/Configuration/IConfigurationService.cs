namespace Kwytka.Configuration;

public interface IConfigurationService
{
    ConfigurationData Data { get; }

    Task LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(CancellationToken cancellationToken = default);
}
