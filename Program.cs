using System.Text;
using AuthServer.Configuration;
using AuthServer.Database;
using AuthServer.Database.Interfaces;
using AuthServer.Database.Repositories;
using AuthServer.Database.Repositories.Interfaces;
using AuthServer.Services.Auth.LocalAuth;
using AuthServer.Services.Auth.LocalAuth.Interfaces;
using AuthServer.Services.Auth.SocialAuth;
using AuthServer.Services.Auth.SocialAuth.Background;
using AuthServer.Services.Auth.SocialAuth.Interfaces;
using AuthServer.Services.Cryptography;
using AuthServer.Services.Cryptography.Interfaces;
using AuthServer.Services.Rpc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .WriteTo.Console(LogEventLevel.Information)
    .CreateLogger();

builder.Services.AddLogging(loggingBuilder =>
{
    loggingBuilder.AddConfiguration(builder.Configuration.GetSection("Logging"));
    loggingBuilder.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
    loggingBuilder.AddFilter("Microsoft.EntityFrameworkCore.Infrastructure", LogLevel.Warning);
    loggingBuilder.AddSerilog(dispose: true);
});

var jwtAuthConfig = builder.Configuration.GetSection("JwtAuth").Get<JwtAuthConfig>();

if (jwtAuthConfig == default)
    throw new ArgumentNullException("Failed to find the 'JwtAuth' section in the config file.");

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

if (corsConfig == default)
    throw new ArgumentNullException("Failed to find the 'Cors' section in the config file.");

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

builder.Services.AddDbContext<IEntityContext, EntityContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Primary"), config => config.CommandTimeout(5)));

builder.Services.AddOptions();
builder.Services.Configure<JwtAuthConfig>(builder.Configuration.GetSection("JwtAuth"));
builder.Services.Configure<LocalAuthConfig>(builder.Configuration.GetSection("LocalAuth"));
builder.Services.Configure<SpotifyAuthConfig>(builder.Configuration.GetSection("SpotifyAuth"));
builder.Services.Configure<GoogleAuthConfig>(builder.Configuration.GetSection("GoogleAuth"));

builder.Services.AddHttpClient();
builder.Services.AddGrpc();
builder.Services.AddAuthorization();

builder.Services.AddSingleton<ILocalAuthService, LocalAuthService>();
builder.Services.AddSingleton<IGoogleAuthService, GoogleAuthService>();
builder.Services.AddSingleton<ISpotifyAuthService, SpotifyAuthService>();
builder.Services.AddSingleton<IAuthService, Sha3AuthService>();

builder.Services.AddScoped<ILocalUserRepository, LocalUserRepository>();
builder.Services.AddScoped<ILocalUserRefreshTokenRepository, LocalUserRefreshTokenRepository>();
builder.Services.AddScoped<ISocialUserRepository, SocialUserRepository>();
builder.Services.AddScoped<ISocialUserRefreshTokenRepository, SocialUserRefreshTokenRepository>();

builder.Services.AddHostedService<SocialAuthBackgroundService>();

var app = builder.Build();

app.UseCors(corsConfig.PolicyName);
app.UseGrpcWeb(new GrpcWebOptions { DefaultEnabled = true });
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    app.MapGrpcService<LocalAuthRpcService>();
    app.MapGrpcService<SocialAuthRpcService>();

    endpoints.MapGet("/", httpContext =>
    {
        httpContext.Response.Redirect("https://google.com");
        return Task.CompletedTask;
    });
});

app.Run();
