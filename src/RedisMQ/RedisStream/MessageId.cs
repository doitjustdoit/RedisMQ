using System;

namespace RedisMQ.RedisStream;

public struct MessageId:IComparable<MessageId>
{
    public MessageId(string messageId)
    {
        var msgSplitRes=messageId.Split('-');
        TimeStamp=Convert.ToInt64(msgSplitRes[0]);
        Order=Convert.ToInt64(msgSplitRes[1]);
    }

    public static MessageId MinMessage = new MessageId("0-0");
    public long TimeStamp { get; set; }
    public long Order { get; set; }
    public override string ToString()
    {
        return $"{TimeStamp}-{Order}";
    }

    public int CompareTo(MessageId other)
    {
        if (TimeStamp == other.TimeStamp && Order == other.Order)
            return 0;
        if(TimeStamp > other.TimeStamp)
            return 1;
        if (TimeStamp < other.TimeStamp)
            return -1;
        if (Order > other.Order)
            return 1;
        return -1;
    }
    public static bool operator >(MessageId a, MessageId b)
    {
        return a.CompareTo(b)>0;
    }

    public static bool operator <(MessageId a, MessageId b)
    {
        return a.CompareTo(b)<0;
    }

    public static bool operator ==(MessageId a,MessageId b)
    {
        return a.CompareTo(b)==0;
    }

    public static bool operator !=(MessageId a, MessageId b)
    {
        return !(a == b);
    }
}

public static class MessageIdExtension
{
    public static string GetPreviousMessageId(this MessageId messageId)
    {
        if (messageId.Order == 0)
        {
            return $"{messageId.TimeStamp - 1}-0";
        }

        return $"{messageId.TimeStamp}-{messageId.Order - 1}";
    }
}