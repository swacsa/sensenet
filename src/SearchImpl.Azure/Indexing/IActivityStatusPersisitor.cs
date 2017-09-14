using SenseNet.Search.Indexing;

namespace SenseNet.Search.Azure.Indexing
{
    public interface IActivityStatusPersisitor
    {
        void PutStatus(IIndexingActivityStatus status);

        IIndexingActivityStatus GetStatus();
    }
}