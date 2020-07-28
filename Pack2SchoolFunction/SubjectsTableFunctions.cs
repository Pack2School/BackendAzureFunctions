using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using Pack2SchoolFunction;
using Pack2SchoolFunction.Tables;
using Pack2SchoolFunction.Templates;
using Pack2SchoolFunctions.AzureObjects;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Pack2SchoolFunctions
{
    public static class SubjectsTableFunctions
    {
        private static readonly string classSubjectsPartitionKey = "0";
        private static readonly string NecessityPartitionKey = "1";
        private static readonly string NecessityRowKey = "Necessity";
        private static readonly string SubjectRowKey = "Subjects";
        private static readonly string SubjectPropertyPrefix = "Subject";
        private static readonly string DeviceId = "deviceId";

        [FunctionName("GetOrCreateSubjectsTable")]
        public static async Task<string> GetOrCreateSubjectsTable(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestMessage request,
            [Table("ClassesTable")] CloudTable classesTable, ILogger log)
        {
            SchoolClass newClassInfo = await Utilities.ExtractContent<SchoolClass>(request);
            TableQuerySegment<ClassesTable> classesResult = await CloudTableOperation.getTableEntityAsync<ClassesTable>(classesTable, newClassInfo.teacherName, newClassInfo.grade);

            if (!classesResult.Any())
            {
                SubjectsTableUtilities.CreateClassTable(newClassInfo.ToString());
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


        [FunctionName("Addsubject")]
        public static async Task AddSubject(
             [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestMessage request, ILogger log)
        {
            SubjectRequest addSubjectRequest = await Utilities.ExtractContent<SubjectRequest>(request);
            var subjectsTable = CloudTableOperation.OpenTable(addSubjectRequest.tableName);
            var subjectsNames = CloudTableOperation.getTableEntityAsync<SubjectsTable>(subjectsTable, classSubjectsPartitionKey).Result.First();
            var subjectsNecessity = CloudTableOperation.getTableEntityAsync<SubjectsTable>(subjectsTable, NecessityPartitionKey).Result.First();
            SubjectsTableUtilities.addNewSubject(addSubjectRequest, subjectsNames, subjectsNecessity);
            await CloudTableOperation.AddTableEntity(subjectsTable, subjectsNames, classSubjectsPartitionKey, classSubjectsPartitionKey);
            await CloudTableOperation.AddTableEntity(subjectsTable, subjectsNecessity, NecessityPartitionKey, NecessityPartitionKey);
        }

        [FunctionName("UpdateSubjectNecessity")]
        public static async Task UpdateSubjectNecessity(
           [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestMessage request,
           ILogger log)
        {
            SubjectRequest subjectInformation = await Utilities.ExtractContent<SubjectRequest>(request);
            var subjectsTable = CloudTableOperation.OpenTable(subjectInformation.tableName);
            var subjectsNames = CloudTableOperation.getTableEntityAsync<SubjectsTable>(subjectsTable, classSubjectsPartitionKey).Result.First();
            var subjectsNecessity = CloudTableOperation.getTableEntityAsync<SubjectsTable>(subjectsTable, NecessityPartitionKey).Result.First();
            SubjectsTableUtilities.UpdateSubjectNecessity(subjectsNames, subjectsNecessity, subjectInformation);
            await CloudTableOperation.AddTableEntity<SubjectsTable>(subjectsTable, subjectsNecessity, NecessityPartitionKey, NecessityPartitionKey);
        }

        [FunctionName("GetNecessitySubjects")]
        public static async Task<List<string>> GetNecessitySubjects(
           [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestMessage request,
           ILogger log)
        {
            SubjectRequest subjectInformation = await Utilities.ExtractContent<SubjectRequest>(request);
            var subjectsTable = CloudTableOperation.OpenTable(subjectInformation.tableName);
            var subjectsNames = CloudTableOperation.getTableEntityAsync<SubjectsTable>(subjectsTable, classSubjectsPartitionKey).Result.First();
            var subjectsNecessity = CloudTableOperation.getTableEntityAsync<SubjectsTable>(subjectsTable, NecessityPartitionKey).Result.First();
            return SubjectsTableUtilities.GetNecessitySubjects(subjectsNames, subjectsNecessity);
            return new List<string>();
        }

        [FunctionName("GetMissingSubjects")]
        public static async Task<List<string>> GetMissingSubjects(
           [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestMessage request, [Table("UsersTable")] CloudTable usersTable,
           ILogger log)
        {

            SubjectRequest subjectInformation = await Utilities.ExtractContent<SubjectRequest>(request);
            UsersTable user = CloudTableOperation.getTableEntityAsync<UsersTable>(usersTable, subjectInformation.userId, subjectInformation.userName).Result.Results.First();

            if (user.userType == "parent")
            {
                user = CloudTableOperation.getTableEntityAsync<UsersTable>(usersTable, user.childId).Result.Results.First();
            }

            var subjectsTable = CloudTableOperation.OpenTable(subjectInformation.tableName);
            var subjectsNames = CloudTableOperation.getTableEntityAsync<SubjectsTable>(subjectsTable, classSubjectsPartitionKey).Result.First();
            var subjectsNecessity = CloudTableOperation.getTableEntityAsync<SubjectsTable>(subjectsTable, NecessityPartitionKey).Result.First();
            var studentSubjects = CloudTableOperation.getTableEntityAsync<SubjectsTable>(subjectsTable, user.PartitionKey, user.RowKey).Result.First();
            var misssingSubjects = SubjectsTableUtilities.GetMissingTable(subjectsNames, subjectsNecessity, studentSubjects);

            return misssingSubjects;
        }


        [FunctionName("CreateClass")]
        public static async Task<string> GetOrCreateClassTable(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestMessage request,
            [Table("ClassesTable")] CloudTable classesTable, ILogger log)
        {
            SchoolClass newClassInfo = await Utilities.ExtractContent<SchoolClass>(request);
            TableQuerySegment<ClassesTable> classesResult = await CloudTableOperation.getTableEntityAsync<ClassesTable>(classesTable, newClassInfo.teacherName, newClassInfo.grade);

            if (!classesResult.Any())
            {
                SubjectsTableUtilities.CreateClassTable(newClassInfo.ToString());
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

        [FunctionName("GetAllSubject")]
        public static async Task<List<string>> GetAllSubjects(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestMessage request, ILogger log)
        {
            {
                SubjectRequest addSubjectRequest = await Utilities.ExtractContent<SubjectRequest>(request);
                var subjectsTable = CloudTableOperation.OpenTable(addSubjectRequest.tableName);
                var subjectsRow = CloudTableOperation.getTableEntityAsync<SubjectsTable>(subjectsTable, classSubjectsPartitionKey).Result.First();
                var properties = typeof(SubjectsTable).GetProperties().Where(propInfo => propInfo.Name.Contains(SubjectPropertyPrefix))
                    .Select(propInfo => propInfo.GetValue(subjectsRow)?.ToString()).ToList();
                return properties;
            }
        }

        [FunctionName("UpdateSubjectStickers")]
        public static async Task UpdateSubjectSticker([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestMessage request,
        ILogger log)
        {
            SubjectRequest subjectRequest = await Utilities.ExtractContent<SubjectRequest>(request);
            var subjectsTable = CloudTableOperation.OpenTable(subjectRequest.tableName);
            var userSubjects = CloudTableOperation.getTableEntityAsync<SubjectsTable>(subjectsTable, subjectRequest.userId, SubjectRowKey).Result.First();
            var subjects = CloudTableOperation.getTableEntityAsync<SubjectsTable>(subjectsTable, classSubjectsPartitionKey, SubjectRowKey).Result.First();
            var subjectsNamesDict = typeof(SubjectsTable).GetProperties().ToDictionary(prop => prop.GetValue(subjects, null).ToString(), prop => prop.Name);

            typeof(SubjectsTable).GetProperty(subjectsNamesDict.GetValueOrDefault(subjectRequest.subject)).SetValue(userSubjects, subjectRequest.stickerId);
            await CloudTableOperation.AddTableEntity(subjectsTable, userSubjects, userSubjects.PartitionKey, userSubjects.RowKey);
        }

        [FunctionName("UpdateStudentSubjects")]
        public static async Task UpdateStudentSubjects([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestMessage request, [Table("UsersTable")] CloudTable usersTable,
         ILogger log)
        {
            var newStates = await Utilities.ExtractContent<Dictionary<string, string>>(request);
            var userId = UsersTableUtilities.GetUserIdFromDeviceId(newStates.GetValueOrDefault(DeviceId));
            var subjectsTableName = UsersTableUtilities.GetSubjectsTableNameFromDeviceId(DeviceId);
            var subjectsTable = CloudTableOperation.OpenTable(subjectsTableName);
            newStates.Remove(DeviceId);
            var userSubject = CloudTableOperation.getTableEntityAsync<SubjectsTable>(subjectsTable, userId, SubjectRowKey).Result.First();
            var subjectsState = CloudTableOperation.getTableEntityAsync<SubjectsTable>(subjectsTable, userId, NecessityRowKey).Result.First();
            SubjectsTableUtilities.UpdateSubjectState(subjectsTable, userSubject, subjectsState, newStates);
            await CloudTableOperation.AddTableEntity(subjectsTable, subjectsState, userId, NecessityRowKey);
        }
    }
}