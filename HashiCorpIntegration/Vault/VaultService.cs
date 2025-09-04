using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using VaultSharp;
using VaultSharp.V1.AuthMethods.AppRole;

namespace HashiCorpIntegration.Vault;

public class VaultService : IVaultService
{
    private readonly VaultSettings _vaultSettings;
    private readonly ILogger<VaultService> _logger;
    private readonly IMemoryCache _cache;
    private readonly VaultClient _vaultClient;

    public VaultService(IOptions<VaultSettings> vaultSettings, ILogger<VaultService> logger, IMemoryCache cache)
    {
        _vaultSettings = vaultSettings.Value;
        _logger = logger;
        _cache = cache;
        _vaultClient = CreateVaultClient();
    }

    private VaultClient CreateVaultClient()
    {
        try
        {
            var vaultClientSettings = new VaultClientSettings(_vaultSettings.VaultUrl, new AppRoleAuthMethodInfo(_vaultSettings.RoleId, _vaultSettings.SecretId));
            return new VaultClient(vaultClientSettings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Vault client");
            throw;
        }
    }

    public async Task<string> GetSecretAsync(string path, string key)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be null or empty", nameof(path));
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or empty", nameof(key));

        var cacheKey = $"vault_{path}_{key}";

        if (_cache.TryGetValue(cacheKey, out string cachedValue))
        {
            _logger.LogDebug("Retrieved secret from cache: {Path}/{Key}", path, key);
            return cachedValue;
        }

        try
        {
            var secret = await _vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(path);

            if (secret?.Data?.Data != null && secret.Data.Data.TryGetValue(key, out var value))
            {
                var secretValue = value.ToString();

                // Cache the secret
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_vaultSettings.CacheExpirationMinutes),
                    Priority = CacheItemPriority.High
                };
                _cache.Set(cacheKey, secretValue, cacheOptions);

                _logger.LogDebug("Retrieved and cached secret: {Path}/{Key}", path, key);
                return secretValue;
            }

            throw new KeyNotFoundException($"Secret key '{key}' not found in path '{path}'");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve secret from Vault: {Path}/{Key}", path, key);
            throw;
        }
    }

    public async Task<Dictionary<string, object>> GetSecretAsync(string path)
    {
        var cacheKey = $"vault_{path}_all";

        if (_cache.TryGetValue(cacheKey, out Dictionary<string, object> cachedValue))
        {
            _logger.LogDebug("Retrieved all secrets from cache: {Path}", path);
            return cachedValue;
        }

        try
        {
            var secret = await _vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(path);

            if (secret?.Data?.Data != null)
            {
                var secrets = new Dictionary<string, object>(secret.Data.Data);

                // Cache the secrets
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_vaultSettings.CacheExpirationMinutes),
                    Priority = CacheItemPriority.High
                };
                _cache.Set(cacheKey, secrets, cacheOptions);

                _logger.LogDebug("Retrieved and cached all secrets: {Path}", path);
                return secrets;
            }

            throw new InvalidOperationException($"No secrets found in path '{path}'");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve secrets from Vault: {Path}", path);
            throw;
        }
    }

    public async Task<string> GetSqlConnectionStringAsync()
    {
        try
        {
            return await GetSecretAsync(_vaultSettings.SqlSecretPath, "connectionstring");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve SQL connection string from Vault");
            throw;
        }
    }
}
