using BlazorFrontEnd.Models;
using System.Net.Http.Json;
using BlazorFrontEnd.Extensions;

namespace BlazorFrontEnd.Services
{
    public class ProductoService
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "api/v1/producto"; // Según el controller

        public ProductoService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<PaginatedList<ProductoDto>?> GetAllAsync(int page = 1, int pageSize = 15, string searchTerm = "")
        {
            try
            {
                var url = $"api/v1/Paginado/productos?page={page}&pageSize={pageSize}&searchTerm={searchTerm}";
                return await _httpClient.GetUnwrappedAsync<PaginatedList<ProductoDto>>(url);
            }
            catch { return null; }
        }

        public async Task<ProductoDto?> GetByIdAsync(string id)
        {
            return await _httpClient.GetUnwrappedAsync<ProductoDto>($"{BaseUrl}/{id}");
        }

        public async Task<bool> CreateAsync(ProductoDto p)
        {
            var res = await CreateWithResultAsync(p);
            return res.Success;
        }

        public async Task<(bool Success, string? ErrorMessage)> CreateWithResultAsync(ProductoDto p)
        {
            var req = new {
                p.Nombre, p.Descripcion, p.CodigoBarras, p.CategoriaId,
                p.MarcaId, ProveedorId = string.IsNullOrEmpty(p.ProveedorId) ? null : p.ProveedorId,
                p.DepositoId, p.PrecioCompra, p.PrecioVenta, p.StockActual, p.StockMinimo
            };

            var response = await _httpClient.PostAsJsonAsync(BaseUrl, req);
            if (response.IsSuccessStatusCode) return (true, null);
            var err = await response.Content.ReadAsStringAsync();
            return (false, err);
        }

        public async Task<bool> UpdateAsync(string id, ProductoDto p)
        {
            var res = await UpdateWithResultAsync(id, p);
            return res.Success;
        }

        public async Task<(bool Success, string? ErrorMessage)> UpdateWithResultAsync(string id, ProductoDto p)
        {
            var req = new {
                Id = id,
                p.Nombre,
                p.Descripcion,
                p.CodigoBarras,
                p.CategoriaId,
                p.MarcaId,
                ProveedorId = string.IsNullOrEmpty(p.ProveedorId) ? null : p.ProveedorId,
                p.DepositoId,
                p.PrecioCompra,
                p.PrecioVenta,
                p.StockMinimo
            };

            var response = await _httpClient.PutAsJsonAsync(BaseUrl, req);
            if (response.IsSuccessStatusCode) return (true, null);
            var err = await response.Content.ReadAsStringAsync();
            return (false, err);
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var response = await _httpClient.DeleteAsync($"{BaseUrl}/{id}");
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> EntradaStockAsync(string id, MovimientoStockRequest req)
        {
            var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/{id}/entrada", req);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> SalidaStockAsync(string id, MovimientoStockRequest req)
        {
            var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/{id}/salida", req);
            return response.IsSuccessStatusCode;
        }
    }
}
