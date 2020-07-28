using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Threading.Tasks;

namespace Pack2SchoolFunctions
{
    public static class CloudTableOperation
    {
        public static readonly string accountName = Environment.GetEnvironmentVariable("AccountName");
        public static readonly string accountKey = Environment.GetEnvironmentVariable("AccountKey");

        public static CloudTable OpenTable(string tableName)
        {
            StorageCredentials creds = new StorageCredentials(accountName, accountKey);
            CloudStorageAccount account = new CloudStorageAccount(creds, useHttps: true);
            CloudTableClient client = account.CreateCloudTableClient();
            CloudTable table = client.GetTableReference(tableName);
            return table;
        }

        public static async Task<CloudTable> CreateTableAsync(string tableName)
        {
            StorageCredentials creds = new StorageCredentials(accountName, accountKey);
            CloudStorageAccount account = new CloudStorageAccount(creds, useHttps: true);
            CloudTableClient client = account.CreateCloudTableClient();
            CloudTable table = client.GetTableReference(tableName);
            await table.CreateAsync();
            return table;
        }

        public static async Task AddTableEntity<T>(CloudTable table, T entity, string partitionKey, string rowKey) where T : TableEntity
        {
            TableOperation insertOperation = TableOperation.InsertOrReplace(entity);
            entity.PartitionKey = partitionKey;
            entity.RowKey = rowKey;
            await table.ExecuteAsync(insertOperation);
        }

        public static async Task<TableQuerySegment<T>> getTableEntityAsync<T>(CloudTable table, string partitionCondition = null, string rowKeyConition = null) where T : TableEntity, new()
        {
            string partitionFilter = null;
            string RowFilter = null;
            string filter;

            if (partitionCondition != null)
            {
                partitionFilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionCondition);
            }
            if (rowKeyConition != null)
            {
                RowFilter = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, rowKeyConition);
            }

            if (partitionFilter != null && RowFilter != null)
            {
                filter = TableQuery.CombineFilters(partitionFilter, TableOperators.And, partitionFilter);
            }
            else if (partitionFilter != null)
            {
                filter = partitionFilter;
            }
            else if (RowFilter != null)
            {

                filter = RowFilter;
            }
            else
            {
                filter = "true";
            }

            TableQuery<T> query = new TableQuery<T>().Where(filter);
            return await table.ExecuteQuerySegmentedAsync(query, null);
        }
    }
}
