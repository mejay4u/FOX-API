using IdCard.Application;
using IdCard.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title       = "ID Card Generation API",
        Version     = "v1",
        Description = "Template-driven, LOB-agnostic member ID card generator. " +
                      "Try: GET /api/idcard/MED-001/MEDICAL"
    });
});

// Wire up Clean Architecture layers
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "ID Card API v1");
    c.RoutePrefix = string.Empty; // Swagger at root URL
});

app.UseHttpsRedirection();
app.MapControllers();
app.Run();
