using Hellang.Middleware.ProblemDetails;
using ProductManagement.API.Common.ProblemDetails;
using ProductManagement.API.Common.Extensions;
using ProductManagement.API.Common.Middleware;
using ProductManagement.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddCors(options => options.AddPolicy("WebClient", policy =>
	policy.WithOrigins("http://localhost:4200").AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddApiProblemDetails();
builder.Services.AddHealthChecks();
builder.Services.AddApiValidation();
builder.Services.AddAuthModule(builder.Configuration, builder.Environment);
builder.Services.AddCaching();
builder.Services.AddSwagger();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseApiMiddleware();
app.UseExceptionHandling();
app.UseCors("WebClient");
app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapControllers();

app.Run();

public partial class Program { }
