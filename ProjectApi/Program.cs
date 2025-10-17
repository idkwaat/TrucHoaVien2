using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ProjectApi.Data;
using ProjectApi.Models;
using ProjectApi.Services;
using System.Text;
using CloudinaryDotNet;
using Microsoft.Extensions.Options;
using ProjectApi.Hubs;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();

// ====================
// 🔹 Cho phép upload file lớn (500MB)
// ====================
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 524_288_000; // 500MB
});
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 524_288_000;
});

// ====================
// 🔹 Cloudinary config
// ====================
builder.Services.Configure<CloudinarySettings>(
    builder.Configuration.GetSection("CloudinarySettings"));

builder.Services.AddSingleton(sp =>
{
    var settings = sp.GetRequiredService<IOptions<CloudinarySettings>>().Value;
    return new Cloudinary(new Account(settings.CloudName, settings.ApiKey, settings.ApiSecret));
});

// ====================
// 🔹 Controllers & Swagger
// ====================
builder.Services.AddControllers(options =>
{
    options.SuppressAsyncSuffixInActionNames = false;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ====================
// 🔹 Database setup
// ====================
var connectionString =
    Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
    throw new Exception("❌ Không tìm thấy connection string!");

builder.Services.AddDbContext<FurnitureDbContext>(options =>
{
    if (connectionString.Contains("postgres", StringComparison.OrdinalIgnoreCase))
        options.UseNpgsql(connectionString);
    else
        options.UseSqlServer(connectionString);
});

// ====================
// 🔹 JWT setup
// ====================
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(30)
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

builder.Services.AddScoped<ITokenService, TokenService>();

// ====================
// 🔹 CORS (Render + local + Vercel)
// ====================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
            "https://truchoavien.vercel.app", // domain Vercel
            "http://localhost:5173",          // local dev
            "http://localhost:3000"           // fallback local
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

var app = builder.Build();


app.UseRouting();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

// ====================
// 🔹 Static files
// ====================
var wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
var uploadsPath = Path.Combine(wwwrootPath, "uploads");
var avatarsPath = Path.Combine(wwwrootPath, "avatars");
Directory.CreateDirectory(uploadsPath);
Directory.CreateDirectory(avatarsPath);

var provider = new FileExtensionContentTypeProvider();
provider.Mappings[".glb"] = "model/gltf-binary";
provider.Mappings[".gltf"] = "model/gltf+json";
provider.Mappings[".bin"] = "application/octet-stream";
provider.Mappings[".fbx"] = "application/octet-stream";
provider.Mappings[".obj"] = "model/obj";
provider.Mappings[".mtl"] = "text/plain";

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(wwwrootPath),
    ContentTypeProvider = provider,
    ServeUnknownFileTypes = true
});

// ====================
// 🔹 Swagger + Controllers + Hubs
// ====================
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.MapHub<PaymentsHub>("/hubs/payments");

// ====================
// 🔹 Auto-migrate database
// ====================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FurnitureDbContext>();
    try
    {
        db.Database.Migrate();
        Console.WriteLine("✅ Database migration applied successfully!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Database migration failed: {ex.Message}");
    }
}

app.Run();
