using System;
using System.Collections.Generic;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;

namespace SenseNet.Search.Azure.Managing
{
    public class AzureIndexManager
    {
        private string _viewName = "";
        private string _datasourceConnectionString = "";
        private static string _apiKey = "";
        private static string _schema = "https://";
        private static string _serviceName = "";
        private static string _indexName = "";
        private static string _dataSourceName = "";
        private static string _indexerName = "";
        private static string _dnsSuffix = "search.windows.net";
        //private static string _apiVersion = "2016-09-01";
        private static int? _operationTimeout = 60;
        //private static int _top = 1000;


        private static DataSourceCredentials _datasourceCredentials;
        private static SearchCredentials _searchCredentials;
        private static ISearchServiceClient _serviceClient;
        private static IIndexesOperations _indices;
        private static IDataSourcesOperations _dataSources;
        private static IIndexersOperations _indexers;

        private DataSource _dataSource;
        private Index _index;
        private Indexer _indexer;
        private DataContainer _container;

        //private Dictionary<string, List<string>> _customHeaders = null;

        public AzureIndexManager(IDataSourcesOperations dataSources, IIndexersOperations indexers,  IIndexesOperations indices)
        {
            //if (_searchCredentials == null)
            //{
            //_index = new Index(_indexName, _fields);
            //_datasourceCredentials = new DataSourceCredentials(_datasourceConnectionString);
            //_container = new DataContainer(_viewName);
            //_dataSource = new DataSource(_dataSourceName, DataSourceType.AzureSql, _datasourceCredentials, _container);
            //_indexer = new Indexer(_indexerName, _dataSourceName, _indexName);
            //_searchCredentials = new SearchCredentials(_apiKey);
            //_serviceClient = new SearchServiceClient(_serviceName, _searchCredentials);
            //_serviceClient.BaseUri = new Uri(_schema + _serviceName + "." + _dnsSuffix); ;
            //_serviceClient.LongRunningOperationRetryTimeout = _operationTimeout;
            //_indices = _serviceClient.Indexes;
            //_dataSources = _serviceClient.DataSources;
            //_indexers = _serviceClient.Indexers;
        //}

            _dataSources = dataSources;
            _indexers = indexers;
            _indices = indices;
        }

        public bool BuildSearchEnvironment()
        {
            var result = false;
            try
            {
                _indices.Create(_index);
                _dataSources.Create(_dataSource);
                _indexers.Create(_indexer);
                _indexers.Run(_indexerName);
                result = true;
            }
            catch (Exception ex)
            {
                // Logging
            }
            return result;
        }

        public bool DemolishSearchEnvironment()
        {
            var result = false;
            try
            {
                _indexers.Delete(_indexerName);
                _dataSources.Delete(_dataSourceName);
                _indices.Delete(_indexName);
                result = true;
            }
            catch (Exception ex)
            {
                // Logging
            }
            return result;
        }


        public bool Reindex()
        {
            var result = false;
            try
            {
                _indexers.Run(_indexerName);
                result = true;
            }
            catch (Exception ex)
            {
                // Logging
            }
            return result;
        }
    }
}