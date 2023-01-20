using Microsoft.Extensions.Options;
using WebApplication1;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.Configure((TestOptions opt )=>
{
    opt.Age = 1;
});
var sec=builder.Configuration.GetSection("Themes:0");

builder.Services.Configure<TestOptions>(sec);
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var options = app.Services.GetService<IOptionsMonitor<TestOptions>>();
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();