using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using CRMS_Peguit.winforms.Auth;
using CRMS_Peguit.winforms.Models.Services;

namespace CRMS_Peguit.winforms.Services
{
    public abstract class BaseApiService : IDisposable
    {
        protected readonly HttpClient Client;
        private readonly bool _disposeClient;

        protected static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        protected BaseApiService(string? baseUrl = null, HttpClient? httpClient = null)
        {
            if (httpClient != null)
            {
                Client = httpClient;
                _disposeClient = false;
            }
            else
            {
                var url = baseUrl ?? DbConfiguration.GetApiBaseUrl();
                if (!url.EndsWith("/")) url += "/";
                Client = new HttpClient { BaseAddress = new Uri(url) };
                _disposeClient = true;
            }
        }

        protected HttpRequestMessage CreateRequest(HttpMethod method, string uri)
        {
            var request = new HttpRequestMessage(method, uri);

            if (!string.IsNullOrWhiteSpace(CurrentSession.JwtToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CurrentSession.JwtToken);
            }

            if (CurrentSession.TenantId > 0)
            {
                request.Headers.TryAddWithoutValidation("X-Tenant-Id", CurrentSession.TenantId.ToString());
            }

            if (CurrentSession.UserId > 0)
            {
                request.Headers.TryAddWithoutValidation("X-User-Id", CurrentSession.UserId.ToString());
            }

            if (!string.IsNullOrWhiteSpace(CurrentSession.Role))
            {
                request.Headers.TryAddWithoutValidation("X-User-Role", CurrentSession.Role);
            }

            return request;
        }

        protected async Task<T?> GetAsync<T>(string uri)
        {
            using var req = CreateRequest(HttpMethod.Get, uri);
            using var resp = await Client.SendAsync(req);
            if (!resp.IsSuccessStatusCode)
            {
                System.Diagnostics.Debug.WriteLine($"[Api GET Error] {uri}: {resp.StatusCode}");
                return default;
            }
            return await resp.Content.ReadFromJsonAsync<T>(JsonOptions);
        }

        protected T? Get<T>(string uri) =>
            GetAsync<T>(uri).GetAwaiter().GetResult();

        protected async Task<TResult?> PostAsync<TBody, TResult>(string uri, TBody body)
        {
            using var req = CreateRequest(HttpMethod.Post, uri);
            req.Content = JsonContent.Create(body, options: JsonOptions);
            using var resp = await Client.SendAsync(req);
            if (!resp.IsSuccessStatusCode)
            {
                System.Diagnostics.Debug.WriteLine($"[Api POST Error] {uri}: {resp.StatusCode}");
                return default;
            }
            return await resp.Content.ReadFromJsonAsync<TResult>(JsonOptions);
        }

        protected TResult? Post<TBody, TResult>(string uri, TBody body) =>
            PostAsync<TBody, TResult>(uri, body).GetAwaiter().GetResult();

        protected async Task<bool> PostAsync<TBody>(string uri, TBody body)
        {
            using var req = CreateRequest(HttpMethod.Post, uri);
            req.Content = JsonContent.Create(body, options: JsonOptions);
            using var resp = await Client.SendAsync(req);
            return resp.IsSuccessStatusCode;
        }

        protected bool Post<TBody>(string uri, TBody body) =>
            PostAsync(uri, body).GetAwaiter().GetResult();

        protected async Task<TResult?> PutAsync<TBody, TResult>(string uri, TBody body)
        {
            using var req = CreateRequest(HttpMethod.Put, uri);
            req.Content = JsonContent.Create(body, options: JsonOptions);
            using var resp = await Client.SendAsync(req);
            if (!resp.IsSuccessStatusCode)
            {
                System.Diagnostics.Debug.WriteLine($"[Api PUT Error] {uri}: {resp.StatusCode}");
                return default;
            }
            return await resp.Content.ReadFromJsonAsync<TResult>(JsonOptions);
        }

        protected TResult? Put<TBody, TResult>(string uri, TBody body) =>
            PutAsync<TBody, TResult>(uri, body).GetAwaiter().GetResult();

        protected async Task<bool> PutAsync<TBody>(string uri, TBody body)
        {
            using var req = CreateRequest(HttpMethod.Put, uri);
            req.Content = JsonContent.Create(body, options: JsonOptions);
            using var resp = await Client.SendAsync(req);
            return resp.IsSuccessStatusCode;
        }

        protected bool Put<TBody>(string uri, TBody body) =>
            PutAsync(uri, body).GetAwaiter().GetResult();

        protected async Task<bool> DeleteAsync(string uri)
        {
            using var req = CreateRequest(HttpMethod.Delete, uri);
            using var resp = await Client.SendAsync(req);
            return resp.IsSuccessStatusCode;
        }

        protected bool Delete(string uri) =>
            DeleteAsync(uri).GetAwaiter().GetResult();

        public virtual void Dispose()
        {
            if (_disposeClient)
            {
                Client?.Dispose();
            }
        }
    }
}
