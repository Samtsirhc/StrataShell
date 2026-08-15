using System.Text.Json;
using System.Text.Json.Serialization;

namespace StrataShell.Core.Configuration;

/// <summary>Atomically loads and saves versioned StrataShell settings.</summary>
public sealed class JsonSettingsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        AllowTrailingCommas = true,
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private readonly string filePath;

    /// <summary>Initializes a settings store at the supplied path.</summary>
    /// <param name="filePath">Absolute or caller-controlled settings file path.</param>
    public JsonSettingsStore(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        this.filePath = Path.GetFullPath(filePath);
    }

    /// <summary>Gets the normalized settings file path.</summary>
    public string FilePath => filePath;

    /// <summary>Loads settings, returning safe defaults when the file is absent.</summary>
    public async Task<StrataSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            return new StrataSettings();
        }

        await using FileStream stream = new(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            4096,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        StrataSettings? settings = await JsonSerializer.DeserializeAsync<StrataSettings>(
            stream,
            SerializerOptions,
            cancellationToken).ConfigureAwait(false);

        return SettingsNormalizer.Normalize(settings);
    }

    /// <summary>
    /// Writes settings through a sibling temporary file and atomically replaces
    /// the destination after all bytes have been flushed.
    /// </summary>
    public async Task SaveAsync(StrataSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        StrataSettings normalized = SettingsNormalizer.Normalize(settings);
        string? directory = Path.GetDirectoryName(filePath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string temporaryPath = filePath + ".tmp";
        try
        {
            await using (FileStream stream = new(
                temporaryPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                4096,
                FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await JsonSerializer.SerializeAsync(
                    stream,
                    normalized,
                    SerializerOptions,
                    cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            File.Move(temporaryPath, filePath, true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }
}
