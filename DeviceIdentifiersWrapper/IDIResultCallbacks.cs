namespace DeviceIdentifiersWrapper
{
    public interface IDIResultCallbacks
    {
        void OnSuccess(string message);
        void OnError(string message);
        void OnDebugStatus(string message);
    }

}