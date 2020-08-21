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



        #region Teacher function 


        [FunctionName("EditSubject")]
        public static async Task<string> EditSubject(
             [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestMessage request, ILogger log)
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

            return JsonConvert.SerializeObject(operationResult);
        }

        [FunctionName("UpdateSubjectNecessity")]
        public static async Task<string> UpdateSubjectNecessity(
         [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestMessage request,
         ILogger log)
        {
            var opeartionResult = new OperationResult();
            SubjectRequest subjectInformation = await Utilities.ExtractContent<SubjectRequest>(request);
            var subjectsTable = CloudTableUtilities.OpenTable(subjectInformation.tableName);
            var subjectsNames = CloudTableUtilities.getTableEntityAsync<SubjectsTable>(subjectsTable, classSubjectsPartitionKey).Result.First();
            var subjectsNecessity = CloudTableUtilities.getTableEntityAsync<SubjectsTable>(subjectsTable, NecessityPartitionKey).Result.First();
            SubjectsTableUtilities.UpdateSubjectNecessity(subjectsNames, subjectsNecessity, subjectInformation);
            await CloudTableUtilities.AddTableEntity<SubjectsTable>(subjectsTable, subjectsNecessity, NecessityPartitionKey, NecessityPartitionKey);
            return JsonConvert.SerializeObject(opeartionResult);
        }



        #endregion

        #region Students and Parents functions


        [FunctionName("GetNeededSubjects")]
        public static async Task<string> GetNeededSubjects(
           [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestMessage request,
           ILogger log)
        {
            var opeartionResult = new OperationResult();
            SubjectRequest subjectInformation = await Utilities.ExtractContent<SubjectRequest>(request);
            var subjectsTable = CloudTableUtilities.OpenTable(subjectInformation.tableName);
            var subjectsNames = CloudTableUtilities.getTableEntityAsync<SubjectsTable>(subjectsTable, classSubjectsPartitionKey).Result.First();
            var subjectsNecessity = CloudTableUtilities.getTableEntityAsync<SubjectsTable>(subjectsTable, NecessityPartitionKey).Result.First();
            var neededSubjects = SubjectsTableUtilities.GetNecessitySubjects(subjectsNames, subjectsNecessity);
            opeartionResult.UpdateData(neededSubjects);
            return JsonConvert.SerializeObject(opeartionResult);
        }


        [FunctionName("GetMissingSubjects")]
        public static async Task<string> GetMissingSubjects(
           [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestMessage request,
           ILogger log)
        {
            OperationResult operationResult = new OperationResult();
            SubjectRequest subjectInformation = await Utilities.ExtractContent<SubjectRequest>(request);
            var subjectsTable = CloudTableUtilities.OpenTable(subjectInformation.tableName);
            var subjectsNames = CloudTableUtilities.getTableEntityAsync<SubjectsTable>(subjectsTable, classSubjectsPartitionKey).Result.First();
            var subjectsNecessity = CloudTableUtilities.getTableEntityAsync<SubjectsTable>(subjectsTable, NecessityPartitionKey).Result.First();
            var studentSubjects = CloudTableUtilities.getTableEntityAsync<SubjectsTable>(subjectsTable, subjectInformation.userId, NecessityRowKey).Result.First();
            var misssingSubjects = SubjectsTableUtilities.GetMissingTable(subjectsNames, subjectsNecessity, studentSubjects);
            operationResult.UpdateData(misssingSubjects);
            return JsonConvert.SerializeObject(operationResult);
        }


        [FunctionName("GetAllSubjects")]
        public static async Task<string> GetAllSubjects(
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
                return JsonConvert.SerializeObject(result);
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

        #endregion

        #region General Functions


//        [FunctionName("ResetClasses")]
        ///public static void ResetClasses([TimerTrigger("0 14 22 * * *")]TimerInfo myTimer, ILogger log)
       // {
            //var classesTables = CloudTableUtilities.OpenTable(ProjectConsts.classesTableName);
            //var tables = CloudTableUtilities.getTableEntityAsync<ClassesTable>(classesTables).Result;
            //foreach (var table in tables)
            //{
            //    var subjectsTable = CloudTableUtilities.OpenTable(table.subjectsTableName);
          //      var necessityRows = CloudTableUtilities.getTableEntityAsync<SubjectsTable>(subjectsTable, rowKeyConition: NecessityRowKey).Result;
        //        foreach (var necessityRow in necessityRows)
      //          {
//                   SubjectsTableUtilities.ResetProperties(necessityRow);
  //                  CloudTableUtilities.AddTableEntity<SubjectsTable>(subjectsTable, necessityRow, necessityRow.PartitionKey, necessityRow.RowKey);
    //            }
//
            //}
     //   }

        //   [FunctionName("SendCommandToScan")]
        //   public static void SendCommandToScan([TimerTrigger("0 39 23 * * *")]TimerInfo myTimer, ILogger log)
        //    {
        //        var classesTables = CloudTableUtilities.OpenTable(ProjectConsts.UsersTableName);
        //        var users = CloudTableUtilities.getTableEntityAsync<UsersTable>(classesTables).Result;
        //        foreach (var user in users)
        //         { 
        //           if (user.UserType == ProjectConsts.StudentType)
        //          {
        //            IotDeviceFunctions.SendCloudToDeviceMessageAsync(ProjectConsts.scanOperaion, user);
        //         }

        //    }
        //      }
        #endregion

    }
}
