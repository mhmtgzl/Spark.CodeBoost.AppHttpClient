using Microsoft.AspNetCore.Http;
using Spark.CodeBoost.JsonSerializer;
using Spark.CodeBoost.ServiceResult;
using System.Collections;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;

namespace Spark.CodeBoost.AppHttpClient;

public class AppHttpClient : IAppHttpClient
{
    private readonly IHttpClientFactory _httpClientFactory;

    public AppHttpClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public Task<Result<T>> GetAsync<T>(string url, CancellationToken cancellationToken = default)
        => SendAsync<T>(url, HttpMethod.Get, null, MediaTypeNames.Application.Json, cancellationToken);

    public Task<Result<T>> PostAsync<T>(string url, object? data, CancellationToken cancellationToken = default)
        => SendAsync<T>(url, HttpMethod.Post, data, MediaTypeNames.Application.Json, cancellationToken);

    public Task<Result<T>> PutAsync<T>(string url, object? data, CancellationToken cancellationToken = default)
        => SendAsync<T>(url, HttpMethod.Put, data, MediaTypeNames.Application.Json, cancellationToken);

    public Task<Result<T>> PatchAsync<T>(string url, object? data, CancellationToken cancellationToken = default)
        => SendAsync<T>(url, HttpMethod.Patch, data, MediaTypeNames.Application.Json, cancellationToken);

    public Task<Result<T>> DeleteAsync<T>(string url, object? data = null, CancellationToken cancellationToken = default)
        => SendAsync<T>(url, HttpMethod.Delete, data, MediaTypeNames.Application.Json, cancellationToken);

    public async Task<Result<T>> PostFormUrlEncodedAsync<T>(string url, Dictionary<string, string> formData, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var content = new FormUrlEncodedContent(formData);
            var response = await client.PostAsync(url, content, cancellationToken);

            var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var charset = response.Content.Headers.ContentType?.CharSet;

            Encoding encoding;
            try
            {
                encoding = string.IsNullOrWhiteSpace(charset)
                    ? Encoding.UTF8
                    : Encoding.GetEncoding(charset);
            }
            catch
            {
                encoding = Encoding.UTF8;
            }

            using var reader = new StreamReader(stream, encoding);
            var body = await reader.ReadToEndAsync();

            if (response.IsSuccessStatusCode)
                return typeof(T) == typeof(string)
                    ? Result<T>.SuccessResult((T)(object)body)
                    : Result<T>.SuccessResult(JsonManager.Deserialize<T>(body)!);

            return Result<T>.FailureResult(body);
        }
        catch (Exception ex)
        {
            return Result<T>.FailureResult($"FormUrlEncoded error: {ex.Message}");
        }
    }


    public Task<Result<T>> PostMultipartAsync<T>(string url, object model, CancellationToken cancellationToken = default)
        => SendAsync<T>(url, HttpMethod.Post, model, "multipart/form-data", cancellationToken);

    private async Task<Result<T>> SendAsync<T>(string url, HttpMethod method, object? body, string contentType, CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var request = BuildRequest(url, method, null, body, contentType);
            var response = await client.SendAsync(request, cancellationToken);

            var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var charset = response.Content.Headers.ContentType?.CharSet;

            Encoding encoding;
            try
            {
                encoding = string.IsNullOrWhiteSpace(charset)
                    ? Encoding.UTF8
                    : Encoding.GetEncoding(charset);
            }
            catch
            {
                encoding = Encoding.UTF8;
            }

            using var reader = new StreamReader(stream, encoding);
            var responseBody = await reader.ReadToEndAsync();

            if (response.IsSuccessStatusCode)
                return Result<T>.SuccessResult(JsonManager.Deserialize<T>(responseBody)!);

            return Result<T>.FailureResult(responseBody);
        }
        catch (Exception ex)
        {
            return Result<T>.FailureResult($"Request error: {ex.Message}");
        }
    }


    private HttpRequestMessage BuildRequest(
        string url,
        HttpMethod method,
        List<(string Key, string Value)>? headers,
        object? body,
        string contentType,
        string? bearerToken = null)
    {
        var request = new HttpRequestMessage(method, url);

        if (!string.IsNullOrWhiteSpace(bearerToken))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        if (headers != null)
        {
            foreach (var (key, value) in headers)
                request.Headers.Add(key, value);
        }

        if (body != null)
        {
            if (contentType == "multipart/form-data")
            {
                request.Content = CreateMultipartContent(body);
            }
            else if (contentType == "application/x-www-form-urlencoded" && body is Dictionary<string, string> formDict)
            {
                request.Content = new FormUrlEncodedContent(formDict);
            }
            else
            {
                request.Content = new StringContent(JsonManager.Serialize(body), Encoding.UTF8, contentType);
            }
        }

        return request;
    }

    private MultipartFormDataContent CreateMultipartContent(object model)
    {
        var content = new MultipartFormDataContent();

        foreach (var prop in model.GetType().GetProperties())
        {
            var value = prop.GetValue(model);
            if (value == null) continue;

            switch (value)
            {
                case IFormFile file:
                    var stream = file.OpenReadStream();
                    var fileContent = new StreamContent(stream);
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
                    content.Add(fileContent, prop.Name, file.FileName);
                    break;

                case IEnumerable<IFormFile> files:
                    foreach (var f in files)
                    {
                        var fs = f.OpenReadStream();
                        var fc = new StreamContent(fs);
                        fc.Headers.ContentType = new MediaTypeHeaderValue(f.ContentType);
                        content.Add(fc, prop.Name, f.FileName);
                    }
                    break;

                case IEnumerable<string> list:
                    foreach (var item in list)
                        content.Add(new StringContent(item), $"{prop.Name}[]");
                    break;

                case IDictionary:
                    content.Add(new StringContent(JsonManager.Serialize(value)), prop.Name);
                    break;

                default:
                    content.Add(new StringContent(value.ToString()!), prop.Name);
                    break;
            }
        }

        return content;
    }

    public string GetCurl(HttpRequestMessage request)
    {
        var method = request.Method.Method;
        var uri = request.RequestUri!.ToString();
        var headers = string.Join(" \\\n  ", request.Headers.Select(h => $"-H \"{h.Key}: {string.Join(", ", h.Value)}\""));
        var content = request.Content?.ReadAsStringAsync().Result ?? string.Empty;
        var contentHeader = request.Content != null ? $"-H \"Content-Type: {request.Content.Headers.ContentType}\"" : string.Empty;
        var data = !string.IsNullOrEmpty(content) ? $"-d '{content}'" : string.Empty;

        return $"curl -X {method} \\\n  {headers} \\\n  {contentHeader} \\\n  {data} \\\n  \"{uri}\"";
    }
}
