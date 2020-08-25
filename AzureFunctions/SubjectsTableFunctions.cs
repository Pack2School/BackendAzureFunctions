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
using System;
using Newtonsoft.Json;
using Microsoft.AspNetCore.JsonPatch.Operations;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;

namespace Pack2SchoolFunctions
{
    public static class SubjectsTableFunctions
    {

        #region Consts 

        private static readonly AzureSignalR SignalR = new AzureSignalR(Environment.GetEnvironmentVariable("AzureSignalRConnectionString"));

        private static readonly string classSubjectsPartitionKey = "0";
        private static readonly string NecessityPartitionKey = "1";
        private static readonly string NecessityRowKey = "Necessity";
        private static readonly string SubjectRowKey = "Subjects";
        private static readonly string SubjectPropertyPrefix = "Subject";

        #endregion

        #region Teacher function 


        [FunctionName("EditSubject")]
        public static async Task<string> EditSubject(
             [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestMessage request, [SignalR(HubName = "Pack2SchoolSignalR1")] IAsyncCollector<SignalRMessage> signalRMessages, ILogger log)
        {
            OperationResult operationResult = new OperationResult();
            SubjectRequest editSubjectRequest = await Utilities.ExtractContent<SubjectRequest>(request);
            var subjectsTable = CloudTableUtilities.OpenTable(editSubjectRequest.tableName);
            var subjectsNames = CloudTableUtilities.getTableEntityAsync<SubjectsTable>(subjectsTable, classSubjectsPartitionKey).Result.First();
            var subjectsNecessity = CloudTableUtilities.getTableEntityAsync<SubjectsTable>(subjectsTable, NecessityPartitionKey).Result.First();

            if (editSubjectRequest.requestType == ProjectConsts.AddSubjectOperation)
            {
                SubjectsTableUtilities.AddNewSubject(editSubjectRequest, subjectsNames, subjectsNecessity, operationResult);
            }

            if (editSubjectRequest.requestType == ProjectConsts.RenameSubjectOperation)
            {
                SubjectsTableUtilities.RenameSubject(editSubjectRequest, subjectsNames, subjectsNecessity, operationResult);
            }

            if (editSubjectRequest.requestType == ProjectConsts.DeleteSubjectOperation)
            {
                SubjectsTableUtilities.DeleteSubject(editSubjectRequest, subjectsNames, subjectsNecessity, operationResult);
            }

            await CloudTableUtilities.AddTableEntity(subjectsTable, subjectsNames, classSubjectsPartitionKey, classSubjectsPartitionKey);
            await CloudTableUtilities.AddTableEntity(subjectsTable, subjectsNecessity, NecessityPartitionKey, NecessityPartitionKey);

            var neededSubjects = SubjectsTableUtilities.GetNeededSubject(editSubjectRequest.tableName);
            var allSubjects = SubjectsTableUtilities.GetAllSubjects(editSubjectRequest.tableName);
            var studentsIds = SubjectsTableUtilities.GetAllStudentsIds(editSubjectRequest.tableName);

            foreach (var studentId in studentsIds)
            {
                var missingSubjects = SubjectsTableUtilities.GetMissingSubejcts(editSubjectRequest.tableName, studentId);


                await signalRMessages.AddAsync(
                new SignalRMessage
                {
                    UserId = studentId,
                    Target = "EditSubject",
                    Arguments = new object[] { neededSubjects, allSubjects, missingSubjects }
                });

            }

            return JsonConvert.SerializeObject(operationResult);
        }

        [FunctionName("UpdateSubjectNecessity")]
        public static async Task<string> UpdateSubjectNecessity(
         [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestMessage request, [SignalR(HubName = "Pack2SchoolSignalR1")] IAsyncCollector<SignalRMessage> signalRMessages,
         ILogger log)
        {
            var opeartionResult = new OperationResult();
            SubjectRequest subjectInformation = await Utilities.ExtractContent<SubjectRequest>(request);
            var subjectsTable = CloudTableUtilities.OpenTable(subjectInformation.tableName);
            var subjectsNames = CloudTableUtilities.getTableEntityAsync<SubjectsTable>(subjectsTable, classSubjectsPartitionKey).Result.First();
            var subjectsNecessity = CloudTableUtilities.getTableEntityAsync<SubjectsTable>(subjectsTable, NecessityPartitionKey).Result.First();
            SubjectsTableUtilities.UpdateSubjectNecessity(subjectsNames, subjectsNecessity, subjectInformation);
            await CloudTableUtilities.AddTableEntity<SubjectsTable>(subjectsTable, subjectsNecessity, NecessityPartitionKey, NecessityPartitionKey);
            var neededSubjects = SubjectsTableUtilities.GetNeededSubject(subjectInformation.tableName);
            var studentsIds = SubjectsTableUtilities.GetAllStudentsIds(subjectInformation.tableName);

            foreach (var studentId in studentsIds)
            {
                var missingSubjects = SubjectsTableUtilities.GetMissingSubejcts(subjectInformation.tableName, studentId);

                await signalRMessages.AddAsync(
                new SignalRMessage
                {
                    UserId = studentId,
                    Target = "UpdateSubjectNecessity",
                    Arguments = new object[] { neededSubjects, missingSubjects }
                });

            }

            return JsonConvert.SerializeObject(opeartionResult);
        }



        #endregion

        #region Students and Parents functions


        [FunctionName("GetNeededSubjects")]
        public static async Task<string> GetNeededSubjects(
           [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestMessage request,
           ILogger log)
        {
            OperationResult operationResult = new OperationResult();
            var opeartionResult = new OperationResult();
            SubjectRequest subjectInformation = await Utilities.ExtractContent<SubjectRequest>(request);
            var neededSubjects = SubjectsTableUtilities.GetNeededSubject(subjectInformation.tableName);
            operationResult.UpdateData(neededSubjects);
            return JsonConvert.SerializeObject(operationResult);
        }


        [FunctionName("GetMissingSubjects")]
        public static async Task<string> GetMissingSubjects(
           [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestMessage request,
           ILogger log)
        {
            OperationResult operationResult = new OperationResult();
            SubjectRequest subjectInformation = await Utilities.ExtractContent<SubjectRequest>(request);
            var missingSubjects = SubjectsTableUtilities.GetMissingSubejcts(subjectInformation.tableName, subjectInformation.userId);
            operationResult.UpdateData(missingSubjects);
            return JsonConvert.SerializeObject(operationResult);
        }


        [FunctionName("GetAllSubjects")]
        public static async Task<string> GetAllSubjects(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestMessage request, ILogger log)
        {
            {
                OperationResult operationResult = new OperationResult();
                SubjectRequest addSubjectRequest = await Utilities.ExtractContent<SubjectRequest>(request);
                var allSubjects = SubjectsTableUtilities.GetAllSubjects(addSubjectRequest.tableName);
                operationResult.UpdateData(allSubjects);
                return JsonConvert.SerializeObject(operationResult);
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

            typeof(SubjectsTable).GetProperty(subjectsNamesDict.GetValueOrDefault(subjectRequest.subjectName)).SetValue(userSubjects, subjectRequest.stickerId);
            await CloudTableUtilities.AddTableEntity(subjectsTable, userSubjects, userSubjects.PartitionKey, userSubjects.RowKey);
        }

        [FunctionName("SendScanOperation")]
        public static async Task SendScanOperation([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestMessage request, ILogger log)
        {
            SubjectRequest subjectRequest = await Utilities.ExtractContent<SubjectRequest>(request);
            var operation = JsonConvert.SerializeObject(new { command = ProjectConsts.scanOperaion });
            await IotDeviceFunctions.SendCloudToDeviceMessageAsync(operation, subjectRequest.userId);
        }

        #endregion

    }
}
