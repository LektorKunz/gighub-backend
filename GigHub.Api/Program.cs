using System.Text;
using System.Text.Json.Serialization;
using GigHub.Api.Data;
using GigHub.Api.Middleware;
using GigHub.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ------------------------------------------------------------------------
// Services (dependency injection-containeren opsættes her - svarer til
// @Configuration/@Bean-opsætning fra Spring, hvis nogen på holdet kender det derfra)
// ------------------------------------------------------------------------

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Enums serialiseres som deres navn ("Koncert") i stedet for deres tal-værdi (0) i JSON.
        // Det matcher, hvordan de gemmes i databasen (se GighubDbContext.OnModelCreating,
        // HasConversion<string>()), og er langt nemmere at læse i Scalar og i Angular's devtools.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Microsoft.AspNetCore.OpenApi - genererer en OpenAPI-beskrivelse af API'et.
// Scalar.AspNetCore bruger den beskrivelse til at tegne en interaktiv test-UI i browseren
// (Swashbuckle/Swagger UI er ikke default i .NET 9+ længere, se design-brief.md afsnit 3).
builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' mangler i appsettings.json.");

builder.Services.AddDbContext<GighubDbContext>(options => options.UseSqlite(connectionString));

// Service-laget - se Services/-mappen. Scoped, fordi de bruger GighubDbContext (som selv er
// Scoped) - samme levetid pr. HTTP-request som i alle andre "almindelige" ASP.NET Core-opsætninger.
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IReviewService, ReviewService>();

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException(
        "Jwt:Key mangler i appsettings.json. Se README.md - i en rigtig deployment bør denne " +
        "komme fra dotnet user-secrets/miljøvariabler, ikke fra en fil, der committes til git.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    // Kun Angular CLI's dev-server-port er tilladt - se gang 02 i dagsplanen for, hvordan en
    // CORS-fejl ser ud i Network-fanen, FØR den rammer, så den ikke fejltolkes som "API'et er nede".
    options.AddPolicy("AngularDev", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

// ------------------------------------------------------------------------
// Seed database ved opstart (kun i Development - i produktion bør migrations køres eksplicit
// som et separat deployment-trin, ikke automatisk hver gang appen starter).
// ------------------------------------------------------------------------
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<GighubDbContext>();
    await DbSeeder.SeedAsync(context);
}

// ------------------------------------------------------------------------
// Middleware-pipeline. Rækkefølgen betyder noget - se kommentarerne ved hvert led.
// ------------------------------------------------------------------------

// Skal ligge FØRST, så den kan fange exceptions fra alt, der kører efter den
// (routing, auth, controllere, services, EF Core).
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // Scalar UI tilgængelig på /scalar/v1 i udvikling
}

app.UseHttpsRedirection();

// Servererer wwwroot/uploads/events/... offentligt - det er her billeder uploadet via
// POST /api/events/{id}/image (gang 08) bliver tilgængelige fra.
app.UseStaticFiles();

app.UseCors("AngularDev");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Gør Program-klassen synlig uden for assembly'en (fx for WebApplicationFactory<Program>,
// hvis der senere tilføjes integrationstests i GigHub.Api.Tests).
public partial class Program
{
}
