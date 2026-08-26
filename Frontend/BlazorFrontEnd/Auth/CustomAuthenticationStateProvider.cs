using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace BlazorFrontEnd.Auth
{
    public class CustomAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly ILocalStorageService _localStorage;
        private readonly TokenStorageService _tokenStorage;

        public string? RealRole { get; private set; }
        public string? CurrentRole => _tokenStorage.SimulatedRole ?? RealRole;
        public bool IsRealAdmin => RealRole?.Equals("Admin", StringComparison.OrdinalIgnoreCase) == true;
        public bool IsSimulatingRole => !string.IsNullOrEmpty(_tokenStorage.SimulatedRole) && IsRealAdmin;
        public string? SimulatedRole => _tokenStorage.SimulatedRole;
        public int? ActiveSucursalId => _tokenStorage.ActiveSucursalId;
        public string? ActiveSucursalNombre => _tokenStorage.ActiveSucursalNombre;
        public string? UserSucursalNombre { get; private set; }
        public int? UserSucursalId { get; private set; }

        public CustomAuthenticationStateProvider(ILocalStorageService localStorage, TokenStorageService tokenStorage)
        {
            _localStorage = localStorage;
            _tokenStorage = tokenStorage;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                var token = await _localStorage.GetItemAsync<string>("authToken");

                if (string.IsNullOrWhiteSpace(token))
                {
                    ResetState();
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                }

                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);

                if (jwtToken.ValidTo < DateTime.UtcNow)
                {
                    await _localStorage.RemoveItemAsync("authToken");
                    await _localStorage.RemoveItemAsync("simulatedRole");
                    await _localStorage.RemoveItemAsync("activeSucursalId");
                    await _localStorage.RemoveItemAsync("activeSucursalNombre");
                    _tokenStorage.ClearToken();
                    ResetState();
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                }

                _tokenStorage.SetToken(token);
                ExtractClaimsData(jwtToken);

                // Restore simulated role and active branch from localStorage if Admin
                if (IsRealAdmin)
                {
                    try
                    {
                        var simRole = await _localStorage.GetItemAsync<string>("simulatedRole");
                        if (!string.IsNullOrWhiteSpace(simRole) && !simRole.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                        {
                            _tokenStorage.SetSimulatedRole(simRole);
                        }

                        var actSucId = await _localStorage.GetItemAsync<string>("activeSucursalId");
                        var actSucNombre = await _localStorage.GetItemAsync<string>("activeSucursalNombre");
                        if (int.TryParse(actSucId, out var sId) && sId > 0)
                        {
                            _tokenStorage.SetActiveSucursal(sId, actSucNombre);
                        }
                    }
                    catch { /* localStorage read error ignored */ }
                }

                var claims = BuildEffectiveClaims(jwtToken);
                var identity = new ClaimsIdentity(claims, "jwt", "name", "role");
                var user = new ClaimsPrincipal(identity);

                return new AuthenticationState(user);
            }
            catch (InvalidOperationException)
            {
                ResetState();
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }
        }

        public void NotifyUserAuthentication(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            _tokenStorage.SetToken(token);
            ExtractClaimsData(jwtToken);

            var claims = BuildEffectiveClaims(jwtToken);
            var identity = new ClaimsIdentity(claims, "jwt", "name", "role");
            var user = new ClaimsPrincipal(identity);

            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
        }

        public void NotifyUserLogout()
        {
            _tokenStorage.ClearToken();
            ResetState();
            var authState = Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
            NotifyAuthenticationStateChanged(authState);
        }

        public async Task LogoutAsync()
        {
            try
            {
                await _localStorage.RemoveItemAsync("authToken");
                await _localStorage.RemoveItemAsync("simulatedRole");
                await _localStorage.RemoveItemAsync("activeSucursalId");
                await _localStorage.RemoveItemAsync("activeSucursalNombre");
            }
            catch { /* Ignore */ }

            _tokenStorage.ClearToken();
            ResetState();
            var authState = Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
            NotifyAuthenticationStateChanged(authState);
        }

        public async Task SetSimulatedRoleAsync(string? role)
        {
            if (!IsRealAdmin) return;

            if (string.IsNullOrWhiteSpace(role) || role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                try { await _localStorage.RemoveItemAsync("simulatedRole"); } catch { }
                _tokenStorage.SetSimulatedRole(null);
            }
            else
            {
                try { await _localStorage.SetItemAsync("simulatedRole", role); } catch { }
                _tokenStorage.SetSimulatedRole(role);
            }

            NotifyStateChangedFromCurrentToken();
        }

        public async Task SetActiveSucursalAsync(int? sucursalId, string? sucursalNombre)
        {
            if (!IsRealAdmin) return;

            if (sucursalId.HasValue && sucursalId.Value > 0)
            {
                try
                {
                    await _localStorage.SetItemAsync("activeSucursalId", sucursalId.Value.ToString());
                    await _localStorage.SetItemAsync("activeSucursalNombre", sucursalNombre ?? "");
                }
                catch { }
                _tokenStorage.SetActiveSucursal(sucursalId.Value, sucursalNombre);
            }
            else
            {
                try
                {
                    await _localStorage.RemoveItemAsync("activeSucursalId");
                    await _localStorage.RemoveItemAsync("activeSucursalNombre");
                }
                catch { }
                _tokenStorage.SetActiveSucursal(null, null);
            }

            NotifyStateChangedFromCurrentToken();
        }

        public async Task ResetSimulationAsync()
        {
            try { await _localStorage.RemoveItemAsync("simulatedRole"); } catch { }
            _tokenStorage.SetSimulatedRole(null);
            NotifyStateChangedFromCurrentToken();
        }

        public void SetSimulatedRole(string? role)
        {
            if (!IsRealAdmin) return;
            _tokenStorage.SetSimulatedRole(role);
            NotifyStateChangedFromCurrentToken();
        }

        public void SetActiveSucursal(int? sucursalId, string? sucursalNombre)
        {
            _tokenStorage.SetActiveSucursal(sucursalId, sucursalNombre);
            NotifyStateChangedFromCurrentToken();
        }

        private void NotifyStateChangedFromCurrentToken()
        {
            if (string.IsNullOrWhiteSpace(_tokenStorage.Token)) return;

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(_tokenStorage.Token);
            var claims = BuildEffectiveClaims(jwtToken);
            var identity = new ClaimsIdentity(claims, "jwt", "name", "role");
            var user = new ClaimsPrincipal(identity);

            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
        }

        private void ExtractClaimsData(JwtSecurityToken jwtToken)
        {
            var roleClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "role" || c.Type == ClaimTypes.Role)?.Value;
            RealRole = roleClaim ?? "Usuario";

            var sucursalIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "sucursalId")?.Value;
            UserSucursalId = int.TryParse(sucursalIdClaim, out var sId) ? sId : null;

            UserSucursalNombre = jwtToken.Claims.FirstOrDefault(c => c.Type == "sucursalNombre")?.Value;
        }

        private IEnumerable<Claim> BuildEffectiveClaims(JwtSecurityToken jwtToken)
        {
            var claims = jwtToken.Claims.ToList();

            if (IsRealAdmin && !string.IsNullOrWhiteSpace(_tokenStorage.SimulatedRole))
            {
                claims.RemoveAll(c => c.Type == "role" || c.Type == ClaimTypes.Role);
                claims.Add(new Claim("role", _tokenStorage.SimulatedRole));
                claims.Add(new Claim(ClaimTypes.Role, _tokenStorage.SimulatedRole));
                claims.Add(new Claim("real_role", RealRole ?? "Admin"));
            }

            return claims;
        }

        private void ResetState()
        {
            RealRole = null;
            UserSucursalId = null;
            UserSucursalNombre = null;
        }
    }
}
