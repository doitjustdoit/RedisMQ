namespace RedisMQ.Serialization.MessagePack;

public static class RedisMQOptionsExtension
{
    public static RedisMQOptions UseMessagePack(this RedisMQOptions options)
    {
        options.Extensions.Add(new MessagePackOptionsExtension());
        return options;
    }
}