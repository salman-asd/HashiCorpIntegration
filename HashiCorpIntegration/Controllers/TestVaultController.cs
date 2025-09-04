using HashiCorpIntegration.Data;
using HashiCorpIntegration.Vault;
using Microsoft.AspNetCore.Mvc;

namespace HashiCorpIntegration.Controllers;

public class TestVaultController : Controller
{
    private readonly IVaultService _vaultService;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<TestVaultController> _logger;

    public TestVaultController(
        IVaultService vaultService, 
        ApplicationDbContext dbContext,
        ILogger<TestVaultController> logger)
    {
        _vaultService = vaultService;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var model = new VaultTestResultViewModel();

        try
        {
            model.VaultConnectionString = await _vaultService.GetSqlConnectionStringAsync();
            model.VaultConnectionSuccess = !string.IsNullOrEmpty(model.VaultConnectionString);
            _logger.LogInformation("Successfully retrieved connection string from Vault");
        }
        catch (Exception ex)
        {
            model.VaultConnectionSuccess = false;
            model.VaultError = ex.Message;
            _logger.LogError(ex, "Vault connection test failed");
        }

        try
        {
            model.DatabaseConnectionSuccess = await _dbContext.Database.CanConnectAsync();
            _logger.LogInformation("Successfully connected to database");
        }
        catch (Exception ex)
        {
            model.DatabaseConnectionSuccess = false;
            model.DatabaseError = ex.Message;
            _logger.LogError(ex, "Database connection test failed");
        }

        return View(model);
    }
}

public class VaultTestResultViewModel
{
    public bool VaultConnectionSuccess { get; set; }
    public string? VaultConnectionString { get; set; }
    public string? VaultError { get; set; }
    public bool DatabaseConnectionSuccess { get; set; }
    public string? DatabaseError { get; set; }
}
