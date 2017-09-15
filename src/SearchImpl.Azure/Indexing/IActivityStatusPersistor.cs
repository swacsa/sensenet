using SenseNet.Search.Indexing;

namespace SenseNet.Search.Azure.Indexing
{
    public interface IActivityStatusPersistor
    {
        void PutStatus(IIndexingActivityStatus status);

        IIndexingActivityStatus GetStatus();
    }
}