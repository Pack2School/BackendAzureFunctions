
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Table;
using Pack2SchoolFunction.Tables;
using Pack2SchoolFunction.Templates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Runtime.InteropServices.WindowsRuntime;
using Newtonsoft.Json;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using System.Reflection;

namespace Pack2SchoolFunction
{
    public static class Functions
    {
        private static readonly string accountName = Environment.GetEnvironmentVariable("AccountName");
        private static readonly string accountKey = Environment.GetEnvironmentVariable("AccountKey");



        // User: all
        // Purpose: To create new account
        [FunctionName("SignUp")]
        public static async Task<Response<UserRequest>> SignUp(
             [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestMessage request,
             [Table("UsersTable")] CloudTable usersTable, ILogger log)
        {
            Response<UserRequest> response = new Response<UserRequest>();
            response.requestSucceeded = true;
            UserRequest newUserRequest = await Utilities.ExtractContent<UserRequest>(request);

            TableQuery<UsersTable> query = new TableQuery<UsersTable>();

            TableQuerySegment<UsersTable> usersResult = await usersTable.ExecuteQuerySegmentedAsync(query, null);

            foreach (UsersTable user in usersResult.Results)
            {
                if (user.PartitionKey == newUserRequest.userId)
                {
                    response.errorMessage = Response<UserRequest>.duplicateId;
                    response.requestSucceeded = false;
                    return response;
                }
            }

            foreach (UsersTable user in usersResult.Results)
            {
                if (user.userEmail == newUserRequest.userEmail)
                {
                    response.errorMessage = Response<UserRequest>.duplicateEmail;
                    response.requestSucceeded = false;
                    return response;
                }
            }

            if (newUserRequest.userType == "parent")
            {
                bool foundChild = false;

                foreach (UsersTable user in usersResult.Results)
                {
                    if (user.PartitionKey == newUserRequest.childId)
                    {
                        foundChild = true;
                    }
                }

                if (!foundChild)
                {
                    response.errorMessage = Response<UserRequest>.childIdNotFound;
                    response.requestSucceeded = false;
                    return response;
                }
            }

            if (response.requestSucceeded)
            {
                UsersTable newUser = new UsersTable() { userEmail = newUserRequest.userName, userPassword = newUserRequest.userPassword, userType = newUserRequest.userType, childId = newUserRequest.childId, subjectsTableName = newUserRequest.subjectsTableName };
                TableOperation insertOperation = TableOperation.InsertOrReplace(newUser);
                newUser.PartitionKey = newUserRequest.userId;
                newUser.RowKey = newUserRequest.userName;
                await usersTable.ExecuteAsync(insertOperation);
            }
            return response;
        }


        // User: all
        // Purpose: to sign in to app
        [FunctionName("SignIn")]
        public static async Task<Response<UserRequest>
            > SignIn(

           [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestMessage request,
           [Table("UsersTable")] CloudTable usersTable, ILogger log)
        {
            Response<UserRequest> response = new Response<UserRequest>();
            UserRequest userRequest = await Utilities.ExtractContent<UserRequest>(request);

            TableQuery<UsersTable> query = new TableQuery<UsersTable>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, userRequest.userId));

            TableQuerySegment<UsersTable> subjectNamequeryResult = await usersTable.ExecuteQuerySegmentedAsync(query, null);

            foreach (var user in subjectNamequeryResult.Results)
            {
                if (user.userPassword == userRequest.userPassword)
                {
                    var userInfo = new UserRequest();
                    userInfo.userType = user.userType;
                    userInfo.subjectsTableName = user.subjectsTableName;
                    response.data = userRequest;
                    response.requestSucceeded = true;
                }
                else
                {
                    response.errorMessage = Response<UserRequest>.wrongPassword;
                    response.requestSucceeded = false;
                }
            }


            if (response.errorMessage == null && response.requestSucceeded == false)
            {
                response.errorMessage = Response<UserRequest>.userNotExist;
                response.requestSucceeded = false;
            }
            return response;
        }

     
        [FunctionName("UpdateStudent")]
        public static async Task UpdateStudent(

           [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestMessage request, [SignalR(HubName = "Pack2SchoolSignalR1")] IAsyncCollector<SignalRMessage> signalRMessages,
           ILogger log)
        {
            var table = Utilities.OpenTable("DemoSubjectsTable", accountName, accountKey);
            string subjectsJson = await request.Content.ReadAsStringAsync();
            Dictionary<string, string> dict = await Utilities.ExtractContent<Dictionary<string, string>>(request);
            var studentEntity = new SubjectsTable();
            var studentsQueryResults = Utilities.getTableEntityAsync<SubjectsTable>(table, "2", "shirShir").Result.First();
            var subjectsQueryResult = Utilities.getTableEntityAsync<SubjectsTable>(table, "1").Result.First();
            studentsQueryResults.SubjectA = GetValueOrDefault(dict, subjectsQueryResult.SubjectA, studentsQueryResults.SubjectA);
            studentsQueryResults.SubjectB = GetValueOrDefault(dict, subjectsQueryResult.SubjectB, studentsQueryResults.SubjectB);
            studentsQueryResults.SubjectC = GetValueOrDefault(dict, subjectsQueryResult.SubjectC, studentsQueryResults.SubjectC);
            studentsQueryResults.SubjectD = GetValueOrDefault(dict, subjectsQueryResult.SubjectD, studentsQueryResults.SubjectD);
            await Utilities.AddTableEntity<SubjectsTable>(table, studentsQueryResults, "2", "shirShir");
            await signalRMessages.AddAsync(
             new SignalRMessage
             {
                 Target = "UpdateStudent",
                 Arguments = new object[] { subjectsJson }
             });
        }

        public static string FromDictionaryToJson(this Dictionary<string, string> dictionary)
        {
            var kvs = dictionary.Select(kvp => string.Format("\"{0}\":\"{1}\"", kvp.Key, string.Concat(",", kvp.Value)));
            return string.Concat("{", string.Join(",", kvs), "}");
        }


        public static string DictionaryToString(Dictionary<string, string> dictionary)
        {
            string dictionaryString = "{";
            foreach (KeyValuePair<string, string> keyValues in dictionary)
            {
                dictionaryString += keyValues.Key + " : " + keyValues.Value + ", ";
            }
            return dictionaryString.TrimEnd(',', ' ') + "}";
        }

        public static string GetValueOrDefault(this Dictionary<string, string> dictionary, string key, string defaultValue)
        {
            string val;
            if (dictionary.TryGetValue(key, out val))
            {
                return val ?? defaultValue;
            }

            return defaultValue;
        }



        // User: Teacher
        // Purpose: create subjects Class Table 
        // status : checked !
        [FunctionName("CreateClass")]
        public static async Task<string> GetOrCreateClassTable(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestMessage request,
            [Table("ClassesTable")] CloudTable classesTable, ILogger log)
        {
            SchoolClass newClassInfo = await Utilities.ExtractContent<SchoolClass>(request);

            TableQuerySegment<ClassesTable> classesResult =  await Utilities.getTableEntityAsync<ClassesTable>(classesTable, newClassInfo.teacherName, newClassInfo.grade);

            if (!classesResult.Any())
            {
                Utilities.CreateClassTable(newClassInfo.ToString(), accountName, accountKey);
                var newClassEntity = new ClassesTable { subjectsTableName = newClassInfo.ToString() };
                TableOperation insertOperation = TableOperation.InsertOrReplace(newClassEntity);
                newClassEntity.PartitionKey = newClassInfo.teacherName;
                newClassEntity.RowKey = newClassInfo.grade;
                await classesTable.ExecuteAsync(insertOperation);
                return newClassEntity.subjectsTableName;
            }

            else
            {
                var classEntity = classesResult.Results.First();
                return classEntity.subjectsTableName;
            }
        }

        // User: Teacher
        // Purpose: add new subject to subjects table 
        // status : checked !
        [FunctionName("Addsubject")]
        public static async Task AddSubject(
             [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestMessage request, ILogger log)
        {
            SubjectRequest addSubjectRequest = await Utilities.ExtractContent<SubjectRequest>(request);

            var subjectsTable = Utilities.OpenTable(addSubjectRequest.tableName, accountName, accountKey);

            var subjectsNames =  Utilities.getTableEntityAsync<SubjectsTable>(subjectsTable, "0").Result.First();

            var subjectsNecessity = Utilities.getTableEntityAsync<SubjectsTable>(subjectsTable, "1").Result.First();

            Utilities.addNewSubject(addSubjectRequest, subjectsNames, subjectsNecessity);

            await Utilities.AddTableEntity(subjectsTable, subjectsNames, "0", "0");

            await Utilities.AddTableEntity(subjectsTable, subjectsNecessity, "1", "1");

        }

        // User: Teacher
        // Purpose: to update necessity subject
        [FunctionName("UpdateSubjectNecessity")]
        public static async Task UpdateSubjectNecessity(
           [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestMessage request,
           ILogger log)
        {
            SubjectRequest subjectInformation = await Utilities.ExtractContent<SubjectRequest>(request);

            var subjectsTable = Utilities.OpenTable(subjectInformation.tableName, accountName, accountKey);

            var subjectsNames = Utilities.getTableEntityAsync<SubjectsTable>(subjectsTable, "0").Result.First();

            var subjectsNecessity = Utilities.getTableEntityAsync<SubjectsTable>(subjectsTable, "1").Result.First();

            Utilities.UpdateSubjectNecessity(subjectsNames, subjectsNecessity, subjectInformation);

            await Utilities.AddTableEntity<SubjectsTable>(subjectsTable, subjectsNecessity, "1", "1");
        }




        // User: Teacher
        // Purpose: to update necessity subject
        [FunctionName("GetMissingSubjects")]
        public static async Task<List<string>> GetMissingSubjects(
           [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestMessage request,
           ILogger log)
        {
            SubjectRequest subjectInformation = await Utilities.ExtractContent<SubjectRequest>(request);

            var subjectsTable = Utilities.OpenTable(subjectInformation.tableName, accountName, accountKey);

            var subjectsNames = Utilities.getTableEntityAsync<SubjectsTable>(subjectsTable, "0").Result.First();

            var subjectsNecessity = Utilities.getTableEntityAsync<SubjectsTable>(subjectsTable, "1").Result.First();

            var studentSubjects = Utilities.getTableEntityAsync<SubjectsTable>(subjectsTable, subjectInformation.userId, subjectInformation.userName).Result.First();

            var misssingSubjects =  Utilities.getMissingTable(subjectsNames, subjectsNecessity, studentSubjects);

            return misssingSubjects;

        }







    }
}

    