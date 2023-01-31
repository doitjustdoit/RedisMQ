using RedisMQ;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddRedisMQ(mqOptions =>
{
    mqOptions.Configuration = ConfigurationOptions.Parse("localhost:6379");
    mqOptions.FailedRetryCount = 3;
    mqOptions.FailedThresholdCallback += message =>
    {
        // 短信 日志 邮件通知。。。
        var topic = message.GetName();
        var group = message.GetGroup();
        var msgId = message.GetId();
        var payload = message.Body;
    };
    // mqOptions.UseMessagePack();
});

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