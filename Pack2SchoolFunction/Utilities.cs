using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Table;
using System.Text;
using Pack2SchoolFunction.Templates;
using Pack2SchoolFunction.Tables;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Microsoft.Azure.EventHubs.Processor;

namespace Pack2SchoolFunction
{
    public static class Utilities
    {
        public static async Task<Dictionary<string, string>> ExtractContent(HttpRequestMessage request)
        {
            string connectionRequestJson = await request.Content.ReadAsStringAsync();
            JObject json = JObject.Parse(connectionRequestJson);
            var values = json.ToObject<Dictionary<string, string>>();
            return values;
        }

        public static async Task<T> ExtractContent<T>(HttpRequestMessage request)
        {
            string connectionRequestJson = await request.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(connectionRequestJson);
        }

        public static string GenerateUserID()
        {
            return Guid.NewGuid().ToString("N");
        }

        public static async void CreateClassTable(string tableName, string accountName, string accountKey)
        {
            StorageCredentials creds = new StorageCredentials(accountName, accountKey);
            CloudStorageAccount account = new CloudStorageAccount(creds, useHttps: true);
            CloudTableClient client = account.CreateCloudTableClient();
            CloudTable table = client.GetTableReference(tableName);
            await table.CreateAsync();
            var subjectNames = new SubjectsTable();
            TableOperation insertOperation = TableOperation.InsertOrReplace(subjectNames);
            subjectNames.PartitionKey = "0";
            subjectNames.RowKey = "0";
            await table.ExecuteAsync(insertOperation);
            var subjectNecessity = new SubjectsTable();
            insertOperation = TableOperation.InsertOrReplace(subjectNecessity);
            subjectNecessity.PartitionKey = "1";
            subjectNecessity.RowKey = "1";
            await table.ExecuteAsync(insertOperation);


        }


        public static CloudTable OpenTable(string tableName, string accountName, string accountKey)
        {
            StorageCredentials creds = new StorageCredentials(accountName, accountKey);
            CloudStorageAccount account = new CloudStorageAccount(creds, useHttps: true);
            CloudTableClient client = account.CreateCloudTableClient();
            CloudTable table = client.GetTableReference(tableName);
            return table;
        }

        public static void addNewSubject(SubjectRequest addSubjectRequest, SubjectsTable subjectsNames, SubjectsTable subjectsNecessity)
        {
            if (subjectsNames.SubjectA == null)
            {
                subjectsNames.SubjectA = addSubjectRequest.Subject;
                subjectsNecessity.SubjectA = " false";
            }
            else if (subjectsNames.SubjectB == null)
            {
                subjectsNames.SubjectB = addSubjectRequest.Subject;
                subjectsNecessity.SubjectB = " false";
            }
            else if (subjectsNames.SubjectC == null)
            {
                subjectsNames.SubjectC = addSubjectRequest.Subject;
                subjectsNecessity.SubjectC = " false";
            }
            else if (subjectsNames.SubjectD == null)
            {
                subjectsNames.SubjectD = addSubjectRequest.Subject;
                subjectsNecessity.SubjectD = " false";
            }
            else if (subjectsNames.SubjectE == null)
            {
                subjectsNames.SubjectE = addSubjectRequest.Subject;
                subjectsNecessity.SubjectE = " false";
            }
            else if (subjectsNames.SubjectF == null)
            {
                subjectsNames.SubjectF = addSubjectRequest.Subject;
                subjectsNecessity.SubjectF = " false";
            }
        }

        public static void UpdateSubjectNecessity(SubjectsTable SubjectNames, SubjectsTable Necessity, SubjectRequest subjectRequest)
        {
            var subjectName = subjectRequest.Subject;

            if (SubjectNames.SubjectA== subjectName)
            {
                Necessity.SubjectA = subjectRequest.needed;
            }
            else if (SubjectNames.SubjectB == subjectName)
            {
                Necessity.SubjectB = subjectRequest.needed;
            }
            else if (SubjectNames.SubjectC == subjectName)
            {
                Necessity.SubjectC = subjectRequest.needed;
            }
            else if (SubjectNames.SubjectD == subjectName)
            {
                Necessity.SubjectD = subjectRequest.needed;
            }
            else if (SubjectNames.SubjectE == subjectName)
            {
                Necessity.SubjectE = subjectRequest.needed;
            }
            else if (SubjectNames.SubjectF == subjectName)
            {
                Necessity.SubjectF = subjectRequest.needed;
            }
        }



        public  async static Task<string>  sendHttpRequest(string baseUri, HttpMethod httpMethod, string data )
        {
            HttpResponseMessage response;
            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage(httpMethod, baseUri))
            {
                using (var stringContent = new StringContent(data, Encoding.UTF8, "application/json"))
                {
                    if (httpMethod == HttpMethod.Post)
                    {
                        response = await client.PostAsync(baseUri, stringContent);
                    }
                    else
                    {
                        response = await client.PostAsync(baseUri, stringContent);
                    }

                    //var contents = await response.Content.ReadAsStringAsync();
                    return data;
                }
            }
        }

        public static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsValidPassword(string password)
        {
            var hasNumber = new Regex(@"[0-9]+");
            var hasUpperChar = new Regex(@"[A-Z]+");
            var hasMinimum8Chars = new Regex(@".{8,}");

            var isValidated = hasNumber.IsMatch(password) && hasUpperChar.IsMatch(password) && hasMinimum8Chars.IsMatch(password);
            return isValidated;
        }

        public static bool EmailExistInSystem(UsersTable tab, string email)
        {
            return false;
        }

        public static List<string> GetNecessitySubjects(SubjectsTable subjects, SubjectsTable subjectsNecessity)
        {

            List<string> neededSubjects = new List<string>();
            if (subjectsNecessity.SubjectA == "yes")
            {
                neededSubjects.Add(subjects.SubjectA);
            }
            else if (subjectsNecessity.SubjectB == "yes")
            {
                neededSubjects.Add(subjects.SubjectB);
            }
            else if (subjectsNecessity.SubjectC == null)
            {
                neededSubjects.Add(subjects.SubjectC);
            }
            else if (subjectsNecessity.SubjectD == "yes")
            {
                neededSubjects.Add(subjects.SubjectD);
            }
            else if (subjectsNecessity.SubjectE == "yes")
            {
                neededSubjects.Add(subjects.SubjectE);
            }
            else if (subjectsNecessity.SubjectF == "yes")
            {
                neededSubjects.Add(subjects.SubjectF);
            }

            return neededSubjects;

        }

        internal static void ResetNecessities(SubjectsTable subjects)
        {
            List<string> neededSubjects = new List<string>();
            if (subjects.SubjectA == "yes")
            {
                neededSubjects.Add(subjects.SubjectA);
            }
            else if (subjects.SubjectB == "yes")
            {
                neededSubjects.Add(subjects.SubjectB);
            }
            else if (subjects.SubjectC == null)
            {
                neededSubjects.Add(subjects.SubjectC);
            }
            else if (subjects.SubjectD == "yes")
            {
                neededSubjects.Add(subjects.SubjectD);
            }
            else if (subjects.SubjectE == "yes")
            {
                neededSubjects.Add(subjects.SubjectE);
            }
            else if (subjects.SubjectF == "yes")
            {
                neededSubjects.Add(subjects.SubjectF);
            }
        }

        public static async Task<TableQuerySegment<T>> getTableEntityAsync<T>(CloudTable table ,string partitionCondition = null, string rowKeyConition = null) where T : TableEntity, new (){ 
            string partitionFilter = null;
            string RowFilter = null;
            string filter;

            if (partitionCondition !=null)
            {
                partitionFilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionCondition );
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

            return  await table.ExecuteQuerySegmentedAsync(query, null);
        }

        public static async Task AddTableEntity<T>(CloudTable table, T entity, string partitionKey, string rowKey) where T : TableEntity
        {
            TableOperation insertOperation = TableOperation.InsertOrReplace(entity);
            entity.PartitionKey = partitionKey;
            entity.RowKey = rowKey;
            await table.ExecuteAsync(insertOperation);

        }

 

        internal static List<string> getMissingTable(SubjectsTable subjectsNames, SubjectsTable subjectsNecessity, SubjectsTable subjectInformation)
        {
            List<string> missingSubjects = new List<string>();

            if (subjectsNecessity.SubjectA == "true") {
                if (subjectInformation.SubjectA == "false")
                {
                    missingSubjects.Add(subjectsNames.SubjectA);
                }
            }
            if (subjectsNecessity.SubjectB == "true")
            {
                if (subjectInformation.SubjectB == "false")
                {
                    missingSubjects.Add(subjectsNames.SubjectB);
                }
            }
            if (subjectsNecessity.SubjectC == "true")
            {
                if (subjectInformation.SubjectC == "false")
                {
                    missingSubjects.Add(subjectsNames.SubjectC);
                }
            }
            if (subjectsNecessity.SubjectD== "true")
            {
                if (subjectInformation.SubjectD == "false")
                {
                    missingSubjects.Add(subjectsNames.SubjectD);
                }
            }
            if (subjectsNecessity.SubjectE == "true")
            {
                if (subjectInformation.SubjectE == "false")
                {
                    missingSubjects.Add(subjectsNames.SubjectE);
                }
            }
            if (subjectsNecessity.SubjectF == "true")
            {
                if (subjectInformation.SubjectF == "false")
                {
                    missingSubjects.Add(subjectsNames.SubjectF);
                }
            }
            return missingSubjects;
        }
    }
}
