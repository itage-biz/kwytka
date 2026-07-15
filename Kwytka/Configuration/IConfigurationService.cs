namespace Kwytka.Configuration;

public interface IConfigurationService
{
    ConfigurationData Data { get; }

    Task LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(ConfigurationData data, CancellationToken cancellationToken = default);

    Task<byte[]> ExportAsync(CancellationToken cancellationToken = default);

    Task ImportAsync(Stream stream, CancellationToken cancellationToken = default);
}
