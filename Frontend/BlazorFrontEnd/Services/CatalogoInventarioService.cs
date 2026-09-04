using BlazorFrontEnd.Models;
using System.Net.Http.Json;
using BlazorFrontEnd.Extensions;

namespace BlazorFrontEnd.Services
{
    public class CatalogoInventarioService
    {
        private readonly HttpClient _httpClient;

        public CatalogoInventarioService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<CategoriaDto>?> GetCategoriasAsync(bool soloActivas = true)
        {
            try
            {
                var res = await _httpClient.GetUnwrappedAsync<PaginatedList<CategoriaDto>>($"api/v1/Categoria?soloActivas={soloActivas}");
                var list = res?.Items ?? new List<CategoriaDto>();
                return soloActivas ? list.Where(c => c.Activo).ToList() : list;
            }
            catch { return new List<CategoriaDto>(); }
        }

        public async Task<List<MarcaDto>?> GetMarcasAsync(bool soloActivas = true)
        {
            try { 
                var res = await _httpClient.GetUnwrappedAsync<PaginatedList<MarcaDto>>($"api/v1/Marca?soloActivas={soloActivas}"); 
                var list = res?.Items ?? new List<MarcaDto>(); 
                return soloActivas ? list.Where(m => m.Activo).ToList() : list;
            }
            catch { return new List<MarcaDto>(); }
        }

        public async Task<List<ProveedorDto>?> GetProveedoresAsync(bool soloActivos = true)
        {
            try { 
                var res = await _httpClient.GetUnwrappedAsync<PaginatedList<ProveedorDto>>($"api/v1/Proveedor?soloActivos={soloActivos}"); 
                var list = res?.Items ?? new List<ProveedorDto>(); 
                return soloActivos ? list.Where(p => p.Activo).ToList() : list;
            }
            catch { return new List<ProveedorDto>(); }
        }

        public async Task<List<DepositoDto>?> GetDepositosAsync(bool soloActivos = true)
        {
            try { 
                var res = await _httpClient.GetUnwrappedAsync<PaginatedList<DepositoDto>>($"api/v1/Deposito?soloActivos={soloActivos}"); 
                var list = res?.Items ?? new List<DepositoDto>(); 
                return soloActivos ? list.Where(d => d.Activo).ToList() : list;
            }
            catch { return new List<DepositoDto>(); }
        }

        // --- Categoria CRUD ---
        public async Task<CategoriaDto?> GetCategoriaByIdAsync(int id) => await _httpClient.GetUnwrappedAsync<CategoriaDto>($"api/v1/Categoria/{id}");
        public async Task<(bool Success, string ErrorMessage)> CreateCategoriaAsync(CategoriaDto dto) 
        { 
            var res = await _httpClient.PostAsJsonAsync("api/v1/Categoria", dto); 
            if (res.IsSuccessStatusCode) return (true, "");
            var err = await res.Content.ReadAsStringAsync();
            return (false, string.IsNullOrWhiteSpace(err) ? "Error al crear la categoría." : err); 
        }
        public async Task<(bool Success, string ErrorMessage)> UpdateCategoriaAsync(int id, CategoriaDto dto) 
        { 
            var res = await _httpClient.PutAsJsonAsync($"api/v1/Categoria", dto); 
            if (res.IsSuccessStatusCode) return (true, "");
            var err = await res.Content.ReadAsStringAsync();
            return (false, string.IsNullOrWhiteSpace(err) ? "Error al actualizar la categoría." : err);
        }
        public async Task<(bool Success, string ErrorMessage)> DeleteCategoriaAsync(int id) 
        { 
            var res = await _httpClient.DeleteAsync($"api/v1/Categoria/{id}"); 
            if (res.IsSuccessStatusCode) return (true, "");
            var err = await res.Content.ReadAsStringAsync();
            return (false, string.IsNullOrWhiteSpace(err) ? "Error al eliminar la categoría." : err);
        }

        // --- Marca CRUD ---
        public async Task<MarcaDto?> GetMarcaByIdAsync(int id) => await _httpClient.GetUnwrappedAsync<MarcaDto>($"api/v1/Marca/{id}");
        public async Task<(bool Success, string ErrorMessage)> CreateMarcaAsync(MarcaDto dto) 
        { 
            var res = await _httpClient.PostAsJsonAsync("api/v1/Marca", dto); 
            if (res.IsSuccessStatusCode) return (true, "");
            var err = await res.Content.ReadAsStringAsync();
            return (false, string.IsNullOrWhiteSpace(err) ? "Error al crear la marca." : err);
        }
        public async Task<(bool Success, string ErrorMessage)> UpdateMarcaAsync(int id, MarcaDto dto) 
        { 
            var res = await _httpClient.PutAsJsonAsync($"api/v1/Marca", dto); 
            if (res.IsSuccessStatusCode) return (true, "");
            var err = await res.Content.ReadAsStringAsync();
            return (false, string.IsNullOrWhiteSpace(err) ? "Error al actualizar la marca." : err);
        }
        public async Task<(bool Success, string ErrorMessage)> DeleteMarcaAsync(int id) 
        { 
            var res = await _httpClient.DeleteAsync($"api/v1/Marca/{id}"); 
            if (res.IsSuccessStatusCode) return (true, "");
            var err = await res.Content.ReadAsStringAsync();
            return (false, string.IsNullOrWhiteSpace(err) ? "Error al eliminar la marca." : err);
        }

        // --- Proveedor CRUD ---
        public async Task<ProveedorDto?> GetProveedorByIdAsync(string id) => await _httpClient.GetUnwrappedAsync<ProveedorDto>($"api/v1/Proveedor/{id}");
        public async Task<(bool Success, string ErrorMessage)> CreateProveedorAsync(ProveedorDto dto)
        {
            var res = await _httpClient.PostAsJsonAsync("api/v1/Proveedor", dto);
            if (res.IsSuccessStatusCode) return (true, "");
            var err = await res.Content.ReadAsStringAsync();
            return (false, string.IsNullOrWhiteSpace(err) ? "Error al guardar el proveedor." : err);
        }
        public async Task<(bool Success, string ErrorMessage)> UpdateProveedorAsync(string id, ProveedorDto dto)
        {
            var res = await _httpClient.PutAsJsonAsync("api/v1/Proveedor", dto);
            if (res.IsSuccessStatusCode) return (true, "");
            var err = await res.Content.ReadAsStringAsync();
            return (false, string.IsNullOrWhiteSpace(err) ? "Error al actualizar el proveedor." : err);
        }
        public async Task<(bool Success, string ErrorMessage)> DeleteProveedorAsync(string id) 
        { 
            var res = await _httpClient.DeleteAsync($"api/v1/Proveedor/{id}"); 
            if (res.IsSuccessStatusCode) return (true, "");
            var err = await res.Content.ReadAsStringAsync();
            return (false, string.IsNullOrWhiteSpace(err) ? "Error al eliminar el proveedor." : err);
        }

        // --- Deposito CRUD ---
        public async Task<DepositoDto?> GetDepositoByIdAsync(int id) => await _httpClient.GetUnwrappedAsync<DepositoDto>($"api/v1/Deposito/{id}");
        public async Task<(bool Success, string ErrorMessage)> CreateDepositoAsync(DepositoDto dto) 
        { 
            var res = await _httpClient.PostAsJsonAsync("api/v1/Deposito", dto); 
            if (res.IsSuccessStatusCode) return (true, "");
            var err = await res.Content.ReadAsStringAsync();
            return (false, string.IsNullOrWhiteSpace(err) ? "Error al crear el depósito." : err);
        }
        public async Task<(bool Success, string ErrorMessage)> UpdateDepositoAsync(int id, DepositoDto dto) 
        { 
            var res = await _httpClient.PutAsJsonAsync($"api/v1/Deposito", dto); 
            if (res.IsSuccessStatusCode) return (true, "");
            var err = await res.Content.ReadAsStringAsync();
            return (false, string.IsNullOrWhiteSpace(err) ? "Error al actualizar el depósito." : err);
        }
        public async Task<(bool Success, string ErrorMessage)> DeleteDepositoAsync(int id) 
        { 
            var res = await _httpClient.DeleteAsync($"api/v1/Deposito/{id}"); 
            if (res.IsSuccessStatusCode) return (true, "");
            var err = await res.Content.ReadAsStringAsync();
            return (false, string.IsNullOrWhiteSpace(err) ? "Error al eliminar el depósito." : err);
        }
    }
}
