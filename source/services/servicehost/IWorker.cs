namespace BuiltSteady.Zaplify.ServiceHost
{
    public interface IWorker
    {
        void Start();
        int Timeout { get; }  // timeout in ms
    }
}
