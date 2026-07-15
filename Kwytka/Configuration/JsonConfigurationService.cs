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

    public ConfigurationData Data => _data.Clone();

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

    public async Task SaveAsync(ConfigurationData data, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(data);
        Validate(data);

        var dataToSave = data.Clone();

        await _lock.WaitAsync(cancellationToken);

        try
        {
            Validate(dataToSave);

            await WriteAsync(dataToSave, cancellationToken);
            _data = dataToSave;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<byte[]> ExportAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);

        try
        {
            return JsonSerializer.SerializeToUtf8Bytes(_data, JsonOptions);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task ImportAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);

        try
        {
            ConfigurationData data;

            try
            {
                data = await JsonSerializer.DeserializeAsync<ConfigurationData>(stream, JsonOptions, cancellationToken)
                       ?? throw new InvalidDataException("Settings file is empty.");
            }
            catch (JsonException exception)
            {
                throw new InvalidDataException("Settings file must contain valid JSON.", exception);
            }

            Validate(data);
            await WriteAsync(data, cancellationToken);
            _data = data;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task WriteAsync(ConfigurationData data, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_configPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = File.Create(_configPath);
        await JsonSerializer.SerializeAsync(stream, data, JsonOptions, cancellationToken);
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
