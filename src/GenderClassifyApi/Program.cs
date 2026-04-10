using System.Net.Http.Headers;
using System.Reflection;
using GenderClassifyApi.Middleware;
using GenderClassifyApi.Services;
using GenderClassifyApi.Validators;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;

EnvironmentFileLoader.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services
    .AddOptions<GenderizeOptions>()
    .Bind(builder.Configuration.GetSection(GenderizeOptions.SectionName))
    .Validate(options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _),
        "Genderize:BaseUrl must be configured with an absolute URI.")
    .Validate(options => options.TimeoutSeconds > 0,
        "Genderize:TimeoutSeconds must be greater than zero.")
    .ValidateOnStart();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Gender Classify API",
        Version = "v1",
        Description = "Classifies names by gender using Genderize.io and returns enriched responses."
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddSingleton<NameParameterValidator>();

builder.Services.AddHttpClient<IGenderizeService, GenderizeService>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<IOptions<GenderizeOptions>>().Value;

    client.BaseAddress = new Uri(options.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
    client.DefaultRequestHeaders.Accept.Add(
        new MediaTypeWithQualityHeaderValue("application/json"));
});

var app = builder.Build();

app.Use(async (context, next) =>
{
    context.Response.Headers["Access-Control-Allow-Origin"] = "*";
    await next();
});

app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Gender Classify API v1");
    options.RoutePrefix = string.Empty;
    options.DocumentTitle = "Gender Classify API Docs";
});
app.UseCors();
app.MapControllers();

app.Run();

public partial class Program;
