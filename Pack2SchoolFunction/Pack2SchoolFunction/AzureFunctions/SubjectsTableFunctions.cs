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

        [FunctionName("EditSubject")]
        public static async Task<OperationResult> EditSubject(
             [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestMessage request, ILogger log)
        {
            OperationResult operationResult = new OperationResult();
            SubjectRequest editSubjectRequest = await Utilities.ExtractContent<SubjectRequest>(request);
            var subjectsTable = CloudTableUtilities.OpenTable(editSubjectRequest.tableName);
            var subjectsNames = CloudTableUtilities.getTableEntityAsync<SubjectsTable>(subjectsTable, classSubjectsPartitionKey).Result.First();
            var subjectsNecessity = CloudTableUtilities.getTableEntityAsync<SubjectsTable>(subjectsTable, NecessityPartitionKey).Result.First();

            if (editSubjectRequest.operation == ProjectConsts.AddSubjectOperation)
            {
                SubjectsTableUtilities.AddNewSubject(editSubjectRequest, subjectsNames, subjectsNecessity, operationResult);
            }

            if (editSubjectRequest.operation == ProjectConsts.RenameSubjectOperation)
            {
                SubjectsTableUtilities.RenameSubject(editSubjectRequest, subjectsNames, subjectsNecessity, operationResult);
            }

            if (editSubjectRequest.operation == ProjectConsts.DeleteSubjectOperation)
            {
                SubjectsTableUtilities.DeleteSubject(editSubjectRequest, subjectsNames, subjectsNecessity, operationResult);
            }

            await CloudTableUtilities.AddTableEntity(subjectsTable, subjectsNames, classSubjectsPartitionKey, classSubjectsPartitionKey);
            await CloudTableUtilities.AddTableEntity(subjectsTable, subjectsNecessity, NecessityPartitionKey, NecessityPartitionKey);

            return operationResult;
        }

        [FunctionName("UpdateSubjectNecessity")]
        public static async Task UpdateSubjectNecessity(
           [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestMessage request,
           ILogger log)
        {
            SubjectRequest subjectInformation = await Utilities.ExtractContent<SubjectRequest>(request);
            var subjectsTable = CloudTableUtilities.OpenTable(subjectInformation.tableName);
            var subjectsNames = CloudTableUtilities.getTableEntityAsync<SubjectsTable>(subjectsTable, classSubjectsPartitionKey).Result.First();
            var subjectsNecessity = CloudTableUtilities.getTableEntityAsync<SubjectsTable>(subjectsTable, NecessityPartitionKey).Result.First();
            SubjectsTableUtilities.UpdateSubjectNecessity(subjectsNames, subjectsNecessity, subjectInformation);
            await CloudTableUtilities.AddTableEntity<SubjectsTable>(subjectsTable, subjectsNecessity, NecessityPartitionKey, NecessityPartitionKey);
        }

        [FunctionName("GetNecessitySubjects")]
        public static async Task<List<string>> GetNecessitySubjects(
           [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestMessage request,
           ILogger log)
        {
            SubjectRequest subjectInformation = await Utilities.ExtractContent<SubjectRequest>(request);
            var subjectsTable = CloudTableUtilities.OpenTable(subjectInformation.tableName);
            var subjectsNames = CloudTableUtilities.getTableEntityAsync<SubjectsTable>(subjectsTable, classSubjectsPartitionKey).Result.First();
            var subjectsNecessity = CloudTableUtilities.getTableEntityAsync<SubjectsTable>(subjectsTable, NecessityPartitionKey).Result.First();
            return SubjectsTableUtilities.GetNecessitySubjects(subjectsNames, subjectsNecessity);
            return new List<string>();
        }

        [FunctionName("GetMissingSubjects")]
        public static async Task<List<string>> GetMissingSubjects(
           [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestMessage request, [Table("UsersTable")] CloudTable usersTable,
           ILogger log)
        {

            SubjectRequest subjectInformation = await Utilities.ExtractContent<SubjectRequest>(request);
            UsersTable user = CloudTableUtilities.getTableEntityAsync<UsersTable>(usersTable, subjectInformation.userId, null).Result.Results.First();

            if (user.UserType == ProjectConsts.ParentType)
            {
            //    user = CloudTableUtilities.getTableEntityAsync<UsersTable>(usersTable, user.childId).Result.Results.First();
            }

            var subjectsTable = CloudTableUtilities.OpenTable(subjectInformation.tableName);
            var subjectsNames = CloudTableUtilities.getTableEntityAsync<SubjectsTable>(subjectsTable, classSubjectsPartitionKey).Result.First();
            var subjectsNecessity = CloudTableUtilities.getTableEntityAsync<SubjectsTable>(subjectsTable, NecessityPartitionKey).Result.First();
            var studentSubjects = CloudTableUtilities.getTableEntityAsync<SubjectsTable>(subjectsTable, user.PartitionKey, user.RowKey).Result.First();
            var misssingSubjects = SubjectsTableUtilities.GetMissingTable(subjectsNames, subjectsNecessity, studentSubjects);

            return misssingSubjects;
        }


        [FunctionName("GetAllSubject")]
        public static async Task<OperationResult> GetAllSubjects(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestMessage request, ILogger log)
        {
            {
                OperationResult result = new OperationResult();
                SubjectRequest addSubjectRequest = await Utilities.ExtractContent<SubjectRequest>(request);
                var subjectsTable = CloudTableUtilities.OpenTable(addSubjectRequest.tableName);
                var subjectsRow = CloudTableUtilities.getTableEntityAsync<SubjectsTable>(subjectsTable, classSubjectsPartitionKey).Result.First();
                var subjects = typeof(SubjectsTable).GetProperties().Where(propInfo => propInfo.Name.Contains(SubjectPropertyPrefix) && propInfo.GetValue(subjectsRow) != null)
                    .Select(propInfo => propInfo.GetValue(subjectsRow)?.ToString()).ToList();
                result.UpdateData(subjects);
                return result;
            }
        }

        [FunctionName("UpdateSubjectStickers")]
        public static async Task UpdateSubjectSticker([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestMessage request,
        ILogger log)
        {
            SubjectRequest subjectRequest = await Utilities.ExtractContent<SubjectRequest>(request);
            var subjectsTable = CloudTableUtilities.OpenTable(subjectRequest.tableName);
            var userSubjects = CloudTableUtilities.getTableEntityAsync<SubjectsTable>(subjectsTable, subjectRequest.userId, SubjectRowKey).Result.First();
            var subjects = CloudTableUtilities.getTableEntityAsync<SubjectsTable>(subjectsTable, classSubjectsPartitionKey, SubjectRowKey).Result.First();
            var subjectsNamesDict = typeof(SubjectsTable).GetProperties().ToDictionary(prop => prop.GetValue(subjects, null).ToString(), prop => prop.Name);

            typeof(SubjectsTable).GetProperty(subjectsNamesDict.GetValueOrDefault(subjectRequest.subject)).SetValue(userSubjects, subjectRequest.stickerId);
            await CloudTableUtilities.AddTableEntity(subjectsTable, userSubjects, userSubjects.PartitionKey, userSubjects.RowKey);
        }

        [FunctionName("UpdateStudentSubjects")]
        public static async Task UpdateStudentSubjects([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestMessage request, [Table("UsersTable")] CloudTable usersTable,
         ILogger log)
        {
            var newStates = await Utilities.ExtractContent<Dictionary<string, string>>(request);
            // var userId = UsersTableUtilities.GetUserIdFromDeviceId(newStates.GetValueOrDefault(DeviceId));
            //   var subjectsTableName = UsersTableUtilities.GetSubjectsTableNameFromDeviceId(DeviceId);
            //   var subjectsTable = CloudTableOperation.OpenTable(subjectsTableName);
            //  newStates.Remove(DeviceId);
            //   var userSubject = CloudTableOperation.getTableEntityAsync<SubjectsTable>(subjectsTable, userId, SubjectRowKey).Result.First();
            ////  var subjectsState = CloudTableOperation.getTableEntityAsync<SubjectsTable>(subjectsTable, userId, NecessityRowKey).Result.First();
            //   SubjectsTableUtilities.UpdateSubjectState(subjectsTable, userSubject, subjectsState, newStates);
            //  await CloudTableOperation.AddTableEntity(subjectsTable, subjectsState, userId, NecessityRowKey);
        }
    }
}