using System.Text.Json;

using Microsoft.Extensions.Options;

namespace Kwytka.Configuration;

public sealed class JsonConfigurationService : IConfigurationService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly string _configPath;
    private ConfigurationData _data = new();

    public JsonConfigurationService(
        IOptions<ConfigurationStorageOptions> storageOptions,
        IHostEnvironment hostEnvironment)
    {
        var configuredPath = storageOptions.Value.ConfigPath;
        _configPath = Path.IsPathFullyQualified(configuredPath)
            ? configuredPath
            : Path.Combine(hostEnvironment.ContentRootPath, configuredPath);
    }

    public ConfigurationData Data => _data;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);

        try
        {
            if (!File.Exists(_configPath))
            {
                _data = new ConfigurationData();
                return;
            }

            await using var stream = File.OpenRead(_configPath);
            var data = await JsonSerializer.DeserializeAsync<ConfigurationData>(stream, JsonOptions, cancellationToken)
                       ?? new ConfigurationData();

            Validate(data);
            _data = data;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);

        try
        {
            Validate(_data);

            var directory = Path.GetDirectoryName(_configPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await using var stream = File.Create(_configPath);
            await JsonSerializer.SerializeAsync(stream, _data, JsonOptions, cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    private static void Validate(ConfigurationData data)
    {
        if (data.PriceLists is null)
        {
            throw new InvalidDataException("Price lists are required.");
        }

        var slugs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var priceList in data.PriceLists)
        {
            if (priceList is null || string.IsNullOrWhiteSpace(priceList.Slug))
            {
                throw new InvalidDataException("Every price list must have a slug.");
            }

            if (!slugs.Add(priceList.Slug))
            {
                throw new InvalidDataException($"Price list slug '{priceList.Slug}' must be unique.");
            }
        }
    }

}
