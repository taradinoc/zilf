using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());
});

builder.Services.AddHttpClient("hp");

var app = builder.Build();
app.UseCors();

app.MapGet("/api/proxy/releases/latest", async (IHttpClientFactory factory, HttpContext ctx) =>
{
    var client = factory.CreateClient("hp");
    var target = "https://foss.heptapod.net/api/v4/projects/zilf%2Fzilf/releases/permalink/latest";
    var req = new HttpRequestMessage(HttpMethod.Get, target);

    // Token sources: query param, header, env var
    var token = ctx.Request.Query["hp_token"].FirstOrDefault()
                ?? ctx.Request.Headers["PRIVATE-TOKEN"].FirstOrDefault()
                ?? ctx.Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "")
                ?? Environment.GetEnvironmentVariable("HeptapodAccessToken");

    if (!string.IsNullOrWhiteSpace(token))
    {
        req.Headers.Add("PRIVATE-TOKEN", token);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    req.Headers.UserAgent.ParseAdd("ZILF-Playground-Proxy/1.0");
    req.Headers.Accept.ParseAdd("application/json");

    var res = await client.SendAsync(req);
    var body = await res.Content.ReadAsStringAsync();
    var contentType = res.Content.Headers.ContentType?.ToString() ?? "application/json";
    
    return Results.Content(body, contentType, statusCode: (int)res.StatusCode);
});

app.Run();
