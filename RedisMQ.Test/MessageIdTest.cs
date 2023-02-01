using RedisMQ.RedisStream;

namespace RedisMQ.Test;

public class MessageIdTest
{
   [Fact]
   public void CompareTest()
   {
      MessageId m1 = new MessageId("123-0");
      MessageId m2 = new MessageId("123-0");
      Assert.Equal(m1,m2);
   }
   [Fact]
   public void CompareTest2()
   {
      MessageId m1 = new MessageId("123-0");
      MessageId m2 = new MessageId("123-1");
      Assert.True(m1<m2);
   }
   [Fact]
   public void CompareTest3()
   {
      MessageId m1 = new MessageId("123-2");
      MessageId m2 = new MessageId("123-1");
      Assert.True(m1>m2);
   }
   [Fact]
   public void CompareTest4()
   {
      MessageId m1 = new MessageId("124-1");
      MessageId m2 = new MessageId("123-1");
      Assert.True(m1>m2);
   }
   [Fact]
   public void CompareTest5()
   {
      MessageId m1 = new MessageId("122-0");
      MessageId m2 = new MessageId("123-1");
      Assert.True(m1<m2);
   }
   [Fact]
   public void CompareTest6()
   {
      MessageId m1 = new MessageId("0-0");
      MessageId m2 = new MessageId("0-0");
      Assert.False(m1<m2);
   }
}