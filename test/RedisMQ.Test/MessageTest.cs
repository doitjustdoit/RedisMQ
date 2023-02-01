using MessagePack;
using RedisMQ.Serialization;
using Microsoft.Extensions.DependencyInjection;
using RedisMQ.Messages;

namespace RedisMQ.Test
{
    public class MessageTest
    {
        private readonly IServiceProvider _provider;

        public MessageTest()
        {
            var services = new ServiceCollection();

            services.AddOptions();
            services.AddSingleton<IServiceCollection>(_ => services);
            services.AddSingleton<ISerializer, JsonUtf8Serializer>();
            _provider = services.BuildServiceProvider();
        }

        [Fact]
        public void Serialize_then_Deserialize_Message_With_Utf8JsonSerializer()
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
            
            // When
            var serializer = _provider.GetRequiredService<ISerializer>();
            var json = serializer.Serialize(givenMessage);
            var deserializedMessage = serializer.Deserialize(json);
            
            // Then
            Assert.True(serializer.IsJsonType(deserializedMessage.Value));
            
            var result = serializer.Deserialize(deserializedMessage.Value, typeof(MessageValue)) as MessageValue;
            Assert.NotNull(result);
            Assert.Equal(result.Email, ((MessageValue)givenMessage.Value).Email);
            Assert.Equal(result.Name, ((MessageValue)givenMessage.Value).Name);
        }
    }
    [MessagePackObject]
    public class MessageValue
    {
        public MessageValue()
        {
            
        }
        public MessageValue(string email, string name)
        {
            Email = email;
            Name = name;
        }
        [Key(0)]
        public string Email { get; set; }
        [Key(1)]
        public string Name { get; set; }
        
        [Key(2)]
        public object? Test { get; set; }
    }
}