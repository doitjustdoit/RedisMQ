using RedisMQ;
using RedisMQ.Serialization.MessagePack;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddRedisMQ(mqOptions =>
{
    mqOptions.Configuration =   ConfigurationOptions.Parse("localhost:6379");
    // use messagepack rather than default json
    // mqOptions.UseMessagePack(builder.Services);

}).WithMessagePack();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();