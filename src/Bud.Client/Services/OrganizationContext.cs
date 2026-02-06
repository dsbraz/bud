using System.Text.Json;
using Bud.Shared.Contracts;
using Microsoft.JSInterop;

namespace Bud.Client.Services;

public sealed class OrganizationContext(IJSRuntime jsRuntime)
{
    private const string StorageKey = "bud.organization.selected";
    private bool _initialized;
    private Guid? _selectedOrganizationId;
    private List<OrganizationSummaryDto> _availableOrganizations = new();

    public event Action? OnOrganizationChanged;

    public Guid? SelectedOrganizationId => _selectedOrganizationId;
    public List<OrganizationSummaryDto> AvailableOrganizations => _availableOrganizations;
    public bool ShowAllOrganizations => _selectedOrganizationId == null;
    public bool IsInitialized => _initialized;

    public async Task InitializeAsync(List<OrganizationSummaryDto> organizations)
    {
        _availableOrganizations = organizations;

        if (!_initialized)
        {
            _initialized = true;
            var storedId = await jsRuntime.InvokeAsync<string>("localStorage.getItem", StorageKey);

            if (!string.IsNullOrWhiteSpace(storedId) && Guid.TryParse(storedId, out var parsedId))
            {
                // Validate that stored org is still available
                if (_availableOrganizations.Any(o => o.Id == parsedId))
                {
                    _selectedOrganizationId = parsedId;
                    return;
                }
            }

            // Default to first organization if available
            if (_availableOrganizations.Count > 0)
            {
                _selectedOrganizationId = _availableOrganizations.First().Id;
            }
        }
    }

    public async Task SelectOrganizationAsync(Guid? organizationId)
    {
        _selectedOrganizationId = organizationId;

        if (organizationId.HasValue)
        {
            await jsRuntime.InvokeAsync<object>("localStorage.setItem", StorageKey, organizationId.Value.ToString());
        }
        else
        {
            await jsRuntime.InvokeAsync<object>("localStorage.removeItem", StorageKey);
        }

        OnOrganizationChanged?.Invoke();
    }

    public async Task ClearAsync()
    {
        _selectedOrganizationId = null;
        _availableOrganizations.Clear();
        await jsRuntime.InvokeAsync<object>("localStorage.removeItem", StorageKey);
    }

    public void UpdateAvailableOrganizations(List<OrganizationSummaryDto> organizations)
    {
        _availableOrganizations = organizations;
        OnOrganizationChanged?.Invoke();
    }

    public string GetSelectedOrganizationName()
    {
        if (_selectedOrganizationId == null)
        {
            return "TODOS";
        }

        return _availableOrganizations
            .FirstOrDefault(o => o.Id == _selectedOrganizationId)?.Name ?? "Desconhecida";
    }
}
