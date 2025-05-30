# AppHttpClient

A lightweight, extensible HTTP client wrapper for .NET applications with standardized response handling.

## Features

- ✔️ Unified `Result<T>` response model
- ✔️ Supports multiple content types:
  - JSON (`application/json`)
  - Form data (`application/x-www-form-urlencoded`)
  - Multipart (`multipart/form-data`) for file uploads
- ✔️ Built-in error handling
- ✔️ HttpClientFactory integration
- ✔️ Curl command generation for debugging
- ✔️ Supports all HTTP methods: GET, POST, PUT, PATCH, DELETE
- ✔️ Custom headers support
- ✔️ Configurable timeouts
- ✔️ Request/response logging

## Installation

### Package Manager
```bash
Install-Package Spark.CodeBoost.AppHttpClient
```
### Registration
```csharp
// Program.cs or Startup.cs
builder.Services.AddHttpClient();  // Required for IHttpClientFactory
builder.Services.AddScoped<IAppHttpClient, AppHttpClient>();

```
## Quick Examples

### Basic GET Request
```csharp
var result = await _httpClient.GetAsync<Product>("https://api.example.com/products/1");

if (result.Success)
{
    Console.WriteLine($"Product: {result.Data.Name}");
}
else
{
    Console.WriteLine($"Error ({result.StatusCode}): {result.Message}");
}

```
### POST with JSON Payload
```csharp
var newItem = new { Name = "New Product", Price = 29.99 };
var result = await _httpClient.PostAsync<Product>(
    "https://api.example.com/products",
    newItem
);

```
### File Upload
```csharp
var result = await _httpClient.PostMultipartAsync<UploadResult>(
    "https://api.example.com/upload",
    new {
        File = fileStream,  // Can be Stream or IFormFile
        Description = "Product image"
    }
);


```
## Advanced Usage

### Custom Headers
```csharp
var request = new HttpRequestMessage(
    HttpMethod.Get,
    "https://api.example.com/secured"
);
request.Headers.Add("X-API-Key", "your-api-key");

var result = await _httpClient.SendAsync<SecureData>(request);

```
### Timeout Configuration
```csharp
// Set per-request timeout (default is 100 seconds)
var result = await _httpClient.GetAsync<Data>(
    "https://api.example.com/large-data",
    timeout: TimeSpan.FromMinutes(3)
);

```
### Debugging with Curl
```csharp
var request = new HttpRequestMessage(
    HttpMethod.Patch,
    "https://api.example.com/products/1"
)
{
    Content = new StringContent(
        JsonSerializer.Serialize(new { Price = 39.99 }),
        Encoding.UTF8,
        "application/json"
    )
};

Console.WriteLine(_httpClient.GenerateCurlCommand(request));
// Output: curl -X PATCH -H "Content-Type: application/json" -d "{\"Price\":39.99}" https://api.example.com/products/1

```
### Response Model
```csharp
public class Result<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
    public HttpStatusCode StatusCode { get; set; }
    public Exception? Exception { get; set; }

    // Helper methods
    public static Result<T> Ok(T data) => new() { 
        Success = true, 
        Data = data,
        StatusCode = HttpStatusCode.OK 
    };
    
    public static Result<T> Fail(string message, HttpStatusCode statusCode) => new() { 
        Success = false, 
        Message = message,
        StatusCode = statusCode 
    };
}

```
## Error Handling

### API-Level Errors (4xx, 5xx)
```csharp
var result = await _httpClient.GetAsync<Data>("...");

if (!result.Success)
{
    Console.WriteLine($"API Error ({result.StatusCode}): {result.Message}");
    
    if (result.StatusCode == HttpStatusCode.NotFound)
    {
        // Handle 404 specifically
    }
}

```
### Network-Level Errors
```csharp
try
{
    var result = await _httpClient.PostAsync<Data>(...);
}
catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.RequestTimeout)
{
    Console.WriteLine("Request timeout occurred");
}
catch (Exception ex)
{
    Console.WriteLine($"Unexpected error: {ex.Message}");
}

```
### Best Practices
### 1. Named Clients
```csharp
builder.Services.AddHttpClient("ProductsAPI", client => 
{
    client.BaseAddress = new Uri("https://api.example.com/products");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.Timeout = TimeSpan.FromSeconds(30);
});

```
### 2. Retry Policies (with Polly)
```csharp
builder.Services.AddHttpClient("RetryClient")
    .AddTransientHttpErrorPolicy(policy => 
        policy.WaitAndRetryAsync(new[]
        {
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(10)
        }));

```
### 3. Logging
```csharp
// Custom logging handler
public class LoggingHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"Request: {request.Method} {request.RequestUri}");
        var response = await base.SendAsync(request, cancellationToken);
        Console.WriteLine($"Response: {(int)response.StatusCode}");
        return response;
    }
}

// Registration
builder.Services.AddHttpClient("LoggedClient")
    .AddHttpMessageHandler<LoggingHandler>();

```
## Contributing
```csharp
1- Fork the repository
2- Create your feature branch (git checkout -b feature/AmazingFeature)
3- Commit your changes (git commit -m 'Add some amazing feature')
4- Push to the branch (git push origin feature/AmazingFeature)
5- Open a Pull Request

```
## License
```bash
Distributed under the MIT License. See LICENSE for more information.
