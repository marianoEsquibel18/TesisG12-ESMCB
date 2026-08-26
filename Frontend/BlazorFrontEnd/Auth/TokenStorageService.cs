namespace BlazorFrontEnd.Auth
{
    /// <summary>
    /// In-memory token and context store that survives across HTTP calls within the same circuit.
    /// Populated after login; used by JwtAuthorizationMessageHandler as fallback
    /// when localStorage (JS interop) is not available during SSR.
    /// Also manages active branch testing and role impersonation context.
    /// </summary>
    public class TokenStorageService
    {
        public string? Token { get; private set; }
        public int? ActiveSucursalId { get; private set; }
        public string? ActiveSucursalNombre { get; private set; }
        public string? SimulatedRole { get; private set; }

        public event Action? OnContextChanged;

        public void SetToken(string token) => Token = token;

        public void ClearToken()
        {
            Token = null;
            ActiveSucursalId = null;
            ActiveSucursalNombre = null;
            SimulatedRole = null;
            OnContextChanged?.Invoke();
        }

        public void SetActiveSucursal(int? sucursalId, string? sucursalNombre)
        {
            ActiveSucursalId = sucursalId;
            ActiveSucursalNombre = sucursalNombre;
            OnContextChanged?.Invoke();
        }

        public void SetSimulatedRole(string? role)
        {
            SimulatedRole = string.IsNullOrWhiteSpace(role) || role.Equals("Admin", StringComparison.OrdinalIgnoreCase) ? null : role;
            OnContextChanged?.Invoke();
        }
    }
}
