
namespace RedisMQ.Test;

public static class AssertExtension
{
    public static void Until(Func<bool> action, int timeout)
    {
        CancellationTokenSource cancel = new(TimeSpan.FromSeconds(timeout));
        try
        {
            while (true)
            {
                cancel.Token.ThrowIfCancellationRequested();
                var res=action();
                if (res)
                {
                    break;
                }

                cancel.Token.WaitHandle.WaitOne(1000);
            }
        }
        catch (OperationCanceledException e)
        {
            throw new Exception("Assert timeout,not meet condition");
        }
        
    }
}