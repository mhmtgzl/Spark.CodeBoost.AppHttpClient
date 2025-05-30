using Spark.CodeBoost.ServiceResult;
using System.Net;

namespace Spark.CodeBoost.AppHttpClient;

/// <summary>
/// Uygulama genelinde HTTP isteklerini kolayca gerçekleştirmek için kullanılır.
/// JSON, Form-UrlEncoded, Multipart destekler. Tüm HTTP metotları için Result<T> tipi döner.
/// </summary>
public interface IAppHttpClient
{
    // JSON destekli istekler
    Task<Result<T>> GetAsync<T>(string url, CancellationToken cancellationToken = default);
    Task<Result<T>> PostAsync<T>(string url, object? data, CancellationToken cancellationToken = default);
    Task<Result<T>> PutAsync<T>(string url, object? data, CancellationToken cancellationToken = default);
    Task<Result<T>> PatchAsync<T>(string url, object? data, CancellationToken cancellationToken = default);
    Task<Result<T>> DeleteAsync<T>(string url, object? data = null, CancellationToken cancellationToken = default);

    // Form-url-encoded istekler (genellikle eski sistemler için)
    Task<Result<T>> PostFormUrlEncodedAsync<T>(string url, Dictionary<string, string> formData, CancellationToken cancellationToken = default);

    // Multipart/form-data istekler (dosya gönderimi)
    Task<Result<T>> PostMultipartAsync<T>(string url, object model, CancellationToken cancellationToken = default);

    // Debug amaçlı CURL çıktısı üretir
    string GetCurl(HttpRequestMessage request);
}
