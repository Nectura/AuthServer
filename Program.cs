using System.Text;
using AuthServer.Configuration;
using AuthServer.Database;
using AuthServer.Services.Auth.External;
using AuthServer.Services.Auth.Local;
using AuthServer.Services.ExternalAuth.Background;
using AuthServer.Services.Rpc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLogging(loggingBuilder =>
{
    loggingBuilder.AddConfiguration(builder.Configuration.GetSection("Logging"));
    loggingBuilder.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
    loggingBuilder.AddFilter("Microsoft.EntityFrameworkCore.Infrastructure", LogLevel.Warning);
    loggingBuilder.AddSerilog();
});

var jwtAuthConfig = builder.Configuration.GetSection("JwtAuth").Get<JwtAuthConfig>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidAudiences = jwtAuthConfig.Audiences,
            ValidIssuer = jwtAuthConfig.Issuer,
            ValidateIssuer = jwtAuthConfig.ValidateIssuer,
            ValidateAudience = jwtAuthConfig.ValidateAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtAuthConfig.PrivateTokenKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(60)
        };
    });

var corsConfig = builder.Configuration.GetSection("Cors").Get<CorsConfig>();

builder.Services.AddCors(options =>
{
    options.AddPolicy(corsConfig.PolicyName,
        policyBuilder =>
        {
            policyBuilder.WithOrigins(corsConfig.Origins)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials()
                .WithExposedHeaders(corsConfig.ExposedHeaders);
        });
});

builder.Services.AddDbContext<EntityContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Primary"), config => config.CommandTimeout(5)));

builder.Services.AddOptions();
builder.Services.Configure<JwtAuthConfig>(builder.Configuration.GetSection("JwtAuth"));
builder.Services.Configure<LocalAuthConfig>(builder.Configuration.GetSection("LocalAuth"));
builder.Services.Configure<SpotifyAuthConfig>(builder.Configuration.GetSection("SpotifyAuth"));
builder.Services.Configure<GoogleAuthConfig>(builder.Configuration.GetSection("GoogleAuth"));

builder.Services.AddHttpClient();
builder.Services.AddGrpc();
builder.Services.AddAuthorization();

builder.Services.AddSingleton<LocalAuthService>();
builder.Services.AddSingleton<GoogleAuthService>();
builder.Services.AddSingleton<SpotifyAuthService>();

builder.Services.AddHostedService<ExternalAuthBackgroundService>();

var app = builder.Build();

app.UseCors(corsConfig.PolicyName);
app.UseGrpcWeb(new GrpcWebOptions { DefaultEnabled = true });
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    app.MapGrpcService<LocalAuthRpcService>();
    app.MapGrpcService<ExternalAuthRpcService>();

    endpoints.MapGet("/", httpContext =>
    {
        httpContext.Response.Redirect("https://google.com");
        return Task.CompletedTask;
    });
});

app.Run();
