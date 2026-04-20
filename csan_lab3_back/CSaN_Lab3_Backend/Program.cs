var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddSingleton<CSaN_Lab3_Backend.Services.FileStorageService>();

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

app.Run();