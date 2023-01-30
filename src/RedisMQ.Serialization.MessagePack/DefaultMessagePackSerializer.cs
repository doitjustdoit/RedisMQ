using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using RedisMQ.Messages;

namespace RedisMQ.Serialization.MessagePack;

public class DefaultMessagePackSerializer:ISerializer
{

    public string Serialize(Message message)
    {
        var bytes=MessagePackSerializer.Serialize(message, ContractlessStandardResolver.Options);
        return   Convert.ToBase64String(bytes);
    }

    public Task<TransportMessage> SerializeAsync(Message message)
    {
        if (message == null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        if (message.Value == null)
        {
            return Task.FromResult(new TransportMessage(message.Headers, null));
        }

        var msgBytes = MessagePackSerializer.Serialize(message.Value, ContractlessStandardResolver.Options);
        return Task.FromResult(new TransportMessage(message.Headers, msgBytes));
    }

    public Message? Deserialize(string sourceBytesBase64)
    {
        byte[] bytes = Convert.FromBase64String(sourceBytesBase64);
        return MessagePackSerializer.Deserialize<Message>(bytes, ContractlessStandardResolver.Options);
    }

    public Task<Message> DeserializeAsync(TransportMessage transportMessage, Type? valueType)
    {
        if (valueType == null || transportMessage.Body == null || transportMessage.Body.Length == 0)
        {
            return Task.FromResult(new Message(transportMessage.Headers, null));
        }

        var obj = MessagePackSerializer.Deserialize(valueType,transportMessage.Body, ContractlessStandardResolver.Options);

        return Task.FromResult(new Message(transportMessage.Headers, obj));
    }

    public object? Deserialize(object value, Type valueType)
    {
        return MessagePackSerializer.Deserialize(valueType,MessagePackSerializer.Serialize(value, ContractlessStandardResolver.Options), ContractlessStandardResolver.Options);
    }

    public bool IsJsonType(object jsonObject)
    {
        return false;
    }
}