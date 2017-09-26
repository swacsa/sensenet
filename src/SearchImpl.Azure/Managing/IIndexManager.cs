namespace SenseNet.Search.Azure.Managing
{
    public interface IIndexManager
    {
        bool BuildSearchEnvironment();

        bool DemolishSearchEnvironment();

        bool Reindex();
    }
}