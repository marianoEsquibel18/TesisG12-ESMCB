using System.Net.Http.Headers;

namespace BlazorFrontEnd.Auth
{
    /// <summary>
    /// Attaches the JWT Bearer token to all outgoing HTTP requests.
    /// Uses only the in-memory TokenStorageService (localStorage/JS interop
    /// is NOT available inside DelegatingHandler since it runs outside the Blazor circuit).
    /// The token is synced to TokenStorageService by MainLayout.OnAfterRenderAsync.
    /// </summary>
    public class JwtAuthorizationMessageHandler : DelegatingHandler
    {
        private readonly TokenStorageService _tokenStorage;

        public JwtAuthorizationMessageHandler(TokenStorageService tokenStorage)
        {
            _tokenStorage = tokenStorage;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = _tokenStorage.Token;

            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            if (_tokenStorage.ActiveSucursalId.HasValue)
            {
                request.Headers.Add("X-Sucursal-Id", _tokenStorage.ActiveSucursalId.Value.ToString());
            }

            if (!string.IsNullOrWhiteSpace(_tokenStorage.SimulatedRole))
            {
                request.Headers.Add("X-Impersonate-Role", _tokenStorage.SimulatedRole);
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
