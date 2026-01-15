using Microsoft.EntityFrameworkCore;
using TyBudget_backend.Data;
using TyBudget_backend.Services;
using TyBudget_backend.BackgroundJobs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
// Service registration is the process of adding reusable components (services) to the Dependency Injection (DI) container.

// Registers MVC controllers with the DI container.Enables support for Web API endpoints defined in controller classes.
builder.Services.AddControllers();

// Registers services needed to generate openAPI docs.
builder.Services.AddOpenApi();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddDbContext<TyBudget_backendDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// Register services
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<IReminderService, ReminderService>();
builder.Services.AddScoped<IRecurringTransactionProcessor, RecurringTransactionProcessor>();
builder.Services.AddScoped<ISubscriptionProcessor, SubscriptionProcessor>();

// Background services
builder.Services.AddHostedService<TyBudget_backendBackgroundService>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

var app = builder.Build();

// Middleware pipeline Configures the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapOpenApi(); // Maps an endpoint for viewing the OpenAPI document in JSON format with the MapOpenApi() method.
}

app.UseHttpsRedirection(); // Redirects HTTP requests to HTTPS to ensure secure communication.
app.UseCors("AllowAll"); // Enables CORS using the "AllowAll" policy defined earlier in the service registration.
app.UseAuthorization(); // Adds authorization middleware to the request pipeline, enforcing access control based on user roles and policies.
app.MapControllers(); // Maps controller action methods to endpoints, enabling them to handle incoming HTTP requests.

app.Run(); // Starts the application and begins listening for incoming HTTP requests.
