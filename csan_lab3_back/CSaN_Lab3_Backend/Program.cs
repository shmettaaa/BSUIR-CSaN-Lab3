using CSaN_Lab3_Backend.Data;
using CSaN_Lab3_Backend.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<FileStorageService>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader()
              .WithMethods("GET", "POST", "PUT", "DELETE", "COPY", "MOVE");
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseHttpsRedirection();
app.MapControllers(); 

using (var scope = app.Services.CreateScope())
{
    var storageService = scope.ServiceProvider.GetRequiredService<FileStorageService>();
    await storageService.SyncWithDiskAsync();
}

app.Run();