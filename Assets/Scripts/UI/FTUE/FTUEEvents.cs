public static class FTUEEvents
{
    public static System.Action<string> OnSignal;

    public static void Emit(string signalName)
    {
        OnSignal?.Invoke(signalName);
    }
}