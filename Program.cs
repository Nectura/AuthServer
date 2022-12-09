using System.Text;
using AuthServer.Configuration;
using AuthServer.Database;
using AuthServer.Database.Interfaces;
using AuthServer.Database.Repositories;
using AuthServer.Database.Repositories.Interfaces;
using AuthServer.Middlewares;
using AuthServer.Services;
using AuthServer.Services.Background;
using AuthServer.Services.Cryptography;
using AuthServer.Services.Cryptography.Interfaces;
using AuthServer.Services.Interfaces;
using AuthServer.Services.Rpc;
using AuthServer.Validators;
using AuthServer.Validators.Interfaces;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Events;
using TwitchLib.Api;
using TwitchLib.Api.Core;
using TwitchLib.Api.Interfaces;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .CreateLogger();

builder.Services.AddLogging(loggingBuilder =>
{
    loggingBuilder.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Information);
    loggingBuilder.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
    loggingBuilder.AddFilter("Microsoft.EntityFrameworkCore.Infrastructure", LogLevel.Warning);
    loggingBuilder.AddSerilog(dispose: true);
});

var googleAuthConfig = builder.Configuration.GetSection("GoogleAuth").Get<GoogleAuthConfig>();
if (googleAuthConfig == default)
    throw new ArgumentNullException(nameof(GoogleAuthConfig), "Failed to find the 'GoogleAuth' section in the config file.");

builder.Services.AddSingleton(new GoogleJsonWebSignature.ValidationSettings
{
    Audience = new[] { googleAuthConfig.JwtAudience },
    ExpirationTimeClockTolerance = TimeSpan.FromSeconds(60),
    IssuedAtClockTolerance = TimeSpan.FromSeconds(60)
});

var twitchConfig = builder.Configuration.GetSection("TwitchAuth").Get<TwitchAuthConfig>();
if (twitchConfig == default)
    throw new ArgumentNullException(nameof(TwitchAuthConfig), "Failed to find the 'TwitchAuth' section in the config file.");

builder.Services.AddSingleton<ITwitchAPI>(new TwitchAPI(settings: new ApiSettings
{
    ClientId = twitchConfig.AuthProviderClientId,
    Secret = twitchConfig.AuthProviderClientSecret
}));

var jwtAuthConfig = builder.Configuration.GetSection("JwtAuth").Get<JwtAuthConfig>();
if (jwtAuthConfig == default)
    throw new ArgumentNullException(nameof(JwtAuthConfig), "Failed to find the 'JwtAuth' section in the config file.");

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
    throw new ArgumentNullException(nameof(CorsConfig), "Failed to find the Cors section in the config file.");

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

var dbConStr = builder.Configuration.GetConnectionString("AuthServer");
if (dbConStr == default)
    throw new ArgumentNullException(nameof(AuthServer), "Failed to find the AuthServer connection string.");

builder.Services.AddDbContext<IEntityContext, EntityContext>(options =>
    options.UseMySql(dbConStr, ServerVersion.AutoDetect(dbConStr))
        .LogTo(Console.WriteLine, LogLevel.Warning)
        //.EnableSensitiveDataLogging()
        .EnableDetailedErrors());

builder.Services.AddOptions();
builder.Services.Configure<JwtAuthConfig>(builder.Configuration.GetSection("JwtAuth"));
builder.Services.Configure<SocialAuthConfig>(builder.Configuration.GetSection("SocialAuth"));
builder.Services.Configure<LocalAuthConfig>(builder.Configuration.GetSection("LocalAuth"));
builder.Services.Configure<SpotifyAuthConfig>(builder.Configuration.GetSection("SpotifyAuth"));
builder.Services.Configure<GoogleAuthConfig>(builder.Configuration.GetSection("GoogleAuth"));
builder.Services.Configure<TwitchAuthConfig>(builder.Configuration.GetSection("TwitchAuth"));
builder.Services.Configure<DiscordAuthConfig>(builder.Configuration.GetSection("DiscordAuth"));

builder.Services.AddHttpClient();
builder.Services.AddGrpc();
builder.Services.AddAuthorization();

builder.Services.AddSingleton<ILocalAuthService, LocalAuthService>();
builder.Services.AddSingleton<IGoogleAuthService, GoogleAuthService>();
builder.Services.AddSingleton<ISpotifyAuthService, SpotifyAuthService>();
builder.Services.AddSingleton<ITwitchAuthService, TwitchAuthService>();
builder.Services.AddSingleton<IDiscordAuthService, DiscordAuthService>();
builder.Services.AddSingleton<ISocialAuthService, SocialAuthService>();
builder.Services.AddSingleton<IAuthService, Sha3AuthService>();
builder.Services.AddTransient<GlobalExceptionMiddleware>();

builder.Services.AddScoped<IUserInfoValidator, UserInfoValidator>();
builder.Services.AddScoped<IPasswordStructureValidator, PasswordStructureValidator>();

builder.Services.AddScoped<ILocalUserRepository, LocalUserRepository>();
builder.Services.AddScoped<ILocalUserRefreshTokenRepository, LocalUserRefreshTokenRepository>();
builder.Services.AddScoped<ISocialUserRepository, SocialUserRepository>();
builder.Services.AddScoped<ISocialUserRefreshTokenRepository, SocialUserRefreshTokenRepository>();
builder.Services.AddScoped<ISocialUserAuthProviderTokenRepository, SocialUserAuthProviderTokenRepository>();

builder.Services.AddHostedService<SocialAuthBackgroundService>();

var app = builder.Build();

app.UseCors(corsConfig.PolicyName);
app.UseGrpcWeb(new GrpcWebOptions { DefaultEnabled = true });
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapGrpcService<LocalAuthRpcService>();
app.MapGrpcService<SocialAuthRpcService>();

app.UseMiddleware<GlobalExceptionMiddleware>();

app.Run();