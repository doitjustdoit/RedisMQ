using System.Text;
using MessagePack;
using Microsoft.Extensions.DependencyInjection;
using RedisMQ.Messages;
using RedisMQ.Serialization;
using RedisMQ.Serialization.MessagePack;

namespace RedisMQ.Test;

public class MessagePackTest
{
    private readonly IServiceProvider _provider;

    public MessagePackTest()
    {
        var services = new ServiceCollection();
        MessagePackSerializer.DefaultOptions=MessagePackSerializerOptions.Standard;
        services.AddOptions();
        services.AddSingleton<IServiceCollection>(_ => services);
        services.AddSingleton<ISerializer, DefaultMessagePackSerializer>();
        _provider = services.BuildServiceProvider();
    }

    [Fact]
    public void Serialize_then_Deserialize_Message_With_MessagePackSerializer()
    {
        // Given  
        var givenMessage = new Message(
            headers: new Dictionary<string, string>() {
                { "cap-msg-name", "authentication.users.update"},
                { "cap-msg-type", "User" },
                { "cap-corr-seq", "0"},
                { "cap-msg-group","service.v1"}
            },
            value: new MessageValue("test@test.com", "User"));
        //     
        // When
        var serializer = _provider.GetRequiredService<ISerializer>();
        var json = serializer.Serialize(givenMessage);
        var deserializedMessage = serializer.Deserialize(json);
            
        // Then
        Assert.False(serializer.IsJsonType(deserializedMessage.Value));
            
        var result = serializer.Deserialize(deserializedMessage.Value, typeof(MessageValue)) as MessageValue;
        Assert.NotNull(result);
        Assert.Equal(result.Email, ((MessageValue)givenMessage.Value).Email);
        Assert.Equal(result.Name, ((MessageValue)givenMessage.Value).Name);
    }
}

