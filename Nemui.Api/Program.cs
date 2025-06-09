using System.Reflection;
using System.Text;
using Asp.Versioning;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Nemui.Api.Middlewares;
using Nemui.Application.Common.Interfaces;
using Nemui.Application.Services;
using Nemui.Application.Validators.Auth;
using Nemui.Infrastructure.Configurations;
using Nemui.Infrastructure.Data.Context;
using Nemui.Infrastructure.Data.Repositories.Implementations;
using Nemui.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
builder.Services.Configure<AuthSettings>(builder.Configuration.GetSection(AuthSettings.SectionName));
builder.Services.Configure<RedisSettings>(builder.Configuration.GetSection(RedisSettings.SectionName));
var redisSettings = builder.Configuration.GetSection(RedisSettings.SectionName).Get<RedisSettings>();
builder.Services.AddStackExchangeRedisCache(options =>
{
    if (redisSettings == null) throw new ArgumentNullException(nameof(redisSettings));
    options.Configuration = redisSettings.GetConnectionString();
    options.InstanceName = redisSettings.InstanceName;
});
builder.Services.AddScoped<ICacheService, RedisCacheService>();
builder.Services.AddScoped<IUserCacheService, UserCacheService>();
builder.Services.AddApiVersioning(options =>
{
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("X-API-Version"),
        new QueryStringApiVersionReader("version"));
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()!;
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = jwtSettings.CreateTokenValidationParameters();
});
builder.Services.AddAuthorization();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IJwtBlacklistService, JwtBlacklistService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Nemui API",
        Version = "v1",
        Description = "API built with .NET",
        Contact = new OpenApiContact
        {
            Name = "QuangTran666",
            Email = "tranducquang.apolos@gmail.com"
        }
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
    
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            []
        }
    });
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "https://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Nemui API v1");
        c.RoutePrefix = string.Empty; // Serve Swagger at root
    });
}
app.UseHttpsRedirection();
app.UseCors("AllowSpecificOrigins");
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<JwtMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
