using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using Microsoft.Azure; // Namespace for CloudConfigurationManager
using Microsoft.WindowsAzure.Storage; // Namespace for CloudStorageAccount
using Microsoft.WindowsAzure.Storage.Table; // Namespace for Table storage

namespace SignalRTimer
{
    public class StorageOnAzureTable
    {
        CloudTable table;

        public StorageOnAzureTable()
        {
            // Retrieve the storage account from the connection string.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                CloudConfigurationManager.GetSetting("StorageConnectionString"));

            // Create the table client.
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            // Retrieve a reference to the table.
            table = tableClient.GetTableReference("sessions");

            // Create the table if it doesn't exist.
            table.CreateIfNotExists();
        }

        internal bool ContainsSession(string sessionid)
        {
            return GetSession(sessionid)!=null;
        }

        internal void SetSession(string sessionid, DateTime endsWhenUtc, string message)
        {
            SessionEntity session = new SessionEntity(sessionid, message);
            session.EndsWhenUtc = endsWhenUtc;

            TableOperation insertOperation = TableOperation.InsertOrReplace(session);

            // Execute the insert operation.
            table.Execute(insertOperation);
        }
        
        internal SessionEntity GetSession(string sessionid)
        {
            TableOperation retrieveOperation = TableOperation.Retrieve<SessionEntity>(sessionid,sessionid);

            TableResult retrievedResult = table.Execute(retrieveOperation);

            if (retrievedResult.Result != null)
            {
                var res = ((SessionEntity)retrievedResult.Result);

                // 24 hours after countdown complete, let it go! remove the session
                if (res.EndsWhenUtc.Subtract(DateTime.UtcNow).TotalHours < -24)
                {
                    return null;
                }

                return res;
            }
            else
            {
                return null;
            }
        }
    }

    public class SessionEntity : TableEntity
    {
        public SessionEntity(string name, string message)
        {
            this.PartitionKey = name;
            this.RowKey = name;
            this.Message = message;
        }

        public SessionEntity() { }

        public DateTime EndsWhenUtc { get; set; }
        public string Message { get; set; }
    }

    public class StorageOnDictionary
    {
        public static Dictionary<string, DateTime> sessionTimouts = new Dictionary<string, DateTime>();

        internal bool ContainsSession(string sessionid)
        {
            return sessionTimouts.ContainsKey(sessionid);
        }

        internal void SetSession(string sessionid, DateTime whenEndsUtc)
        {
            sessionTimouts[sessionid] = whenEndsUtc;
        }

        internal DateTime? GetSession(string sessionid)
        {
            return sessionTimouts[sessionid];
        }
    }

}