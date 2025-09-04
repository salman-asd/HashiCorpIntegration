namespace HashiCorpIntegration.Vault;

public class VaultSettings
{
    public string VaultUrl { get; set; } = string.Empty;
    public string RoleId { get; set; } = string.Empty;
    public string SecretId { get; set; } = string.Empty;
    public string SqlSecretPath { get; set; } = string.Empty;
    public int CacheExpirationMinutes { get; set; } = 30;
}
