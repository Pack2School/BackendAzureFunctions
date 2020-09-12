using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Pack2SchoolFunction;
using Pack2SchoolFunction.Templates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Pack2SchoolFunctions
{
    public static class SubjectsTableFunctions
    {

        #region Teacher function 


        [FunctionName("EditSubject")]
        public static async Task<string> EditSubject(
             [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestMessage request, [SignalR(HubName = "Pack2SchoolSignalR1")] IAsyncCollector<SignalRMessage> signalRMessages, ILogger log)
        {
            OperationResult operationResult = new OperationResult();
            SubjectRequest editSubjectRequest = await Utilities.ExtractContent<SubjectRequest>(request);
            var subjectsTable = CloudTableUtilities.OpenTable(editSubjectRequest.tableName);
            var classSubjectsNames = CloudTableUtilities.getTableEntityAsync<SubjectsTable>(subjectsTable, SubjectsTableUtilities.ClassSubjects).Result.First();
            var classSubjectsNecessity = CloudTableUtilities.getTableEntityAsync<SubjectsTable>(subjectsTable, SubjectsTableUtilities.Necessity).Result.First();
            var classesTable = CloudTableUtilities.OpenTable(ProjectConsts.classesTableName);
            var classesEntities = CloudTableUtilities.getTableEntityAsync<ClassesTable>(classesTable).Result;
            string scheduleTableName = null;

            foreach (var classEntity in classesEntities)
            {

                if (classEntity.subjectsTableName == editSubjectRequest.tableName)
                {
                    scheduleTableName = classEntity.ScheduleTableName;
                    break;
                }
            }

            if (editSubjectRequest.requestType == ProjectConsts.AddSubjectOperation)
            {
                SubjectsTableUtilities.AddNewSubject(editSubjectRequest, classSubjectsNames, classSubjectsNecessity, operationResult);
            }

            if (editSubjectRequest.requestType == ProjectConsts.RenameSubjectOperation)
            {
                await SubjectsTableUtilities.RenameSubjectAsync(editSubjectRequest, classSubjectsNames, scheduleTableName, classSubjectsNecessity, operationResult);
            }

            if (editSubjectRequest.requestType == ProjectConsts.DeleteSubjectOperation)
            {
                await SubjectsTableUtilities.DeleteSubjectAsync(editSubjectRequest, classSubjectsNames, scheduleTableName, classSubjectsNecessity, operationResult);
            }

            await CloudTableUtilities.AddTableEntity(subjectsTable, classSubjectsNames);
            await CloudTableUtilities.AddTableEntity(subjectsTable, classSubjectsNecessity);

            var neededSubjects = SubjectsTableUtilities.GetNeededSubject(editSubjectRequest.tableName);
            var allSubjects = SubjectsTableUtilities.GetAllSubjects(editSubjectRequest.tableName);
            var studentsIds = SubjectsTableUtilities.GetAllStudentsIds(editSubjectRequest.tableName);

            foreach (var studentId in studentsIds)
            {
                var missingSubjects = SubjectsTableUtilities.GetMissingSubejcts(editSubjectRequest.tableName, studentId);

                var dataBaseAndScanUpdates = new DataBaseAndScanUpdates(studentId, neededSubjects, missingSubjects, allSubjects);

                await signalRMessages.AddAsync(
                new SignalRMessage
                {
                    UserId = studentId,
                    Target = ProjectConsts.SignalRTarget,
                    Arguments = new object[] { dataBaseAndScanUpdates }
                }) ;
            }

            return JsonConvert.SerializeObject(operationResult);
        }

        [FunctionName("UpdateSubjectNecessity")]
        public static async Task<string> UpdateSubjectNecessity(
         [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestMessage request, [SignalR(HubName = "Pack2SchoolSignalR1")] IAsyncCollector<SignalRMessage> signalRMessages,
         ILogger log)
        {
            var operationResult = new OperationResult();
            SubjectRequest subjectRequest = await Utilities.ExtractContent<SubjectRequest>(request);
            var subjectsTable = CloudTableUtilities.OpenTable(subjectRequest.tableName);
            var classSubjectsNames = CloudTableUtilities.getTableEntityAsync<SubjectsTable>(subjectsTable, SubjectsTableUtilities.ClassSubjects).Result.First();
            var classSubjectsNecessity = CloudTableUtilities.getTableEntityAsync<SubjectsTable>(subjectsTable, SubjectsTableUtilities.Necessity).Result.First();
            SubjectsTableUtilities.UpdateSubjectNecessity(classSubjectsNames, classSubjectsNecessity, subjectRequest.neededForTomorrow);
            await CloudTableUtilities.AddTableEntity(subjectsTable, classSubjectsNecessity);
            var neededSubjects = SubjectsTableUtilities.GetNeededSubject(subjectRequest.tableName);
            var studentsIds = SubjectsTableUtilities.GetAllStudentsIds(subjectRequest.tableName);

            foreach (var studentId in studentsIds)
            {
                var missingSubjects = SubjectsTableUtilities.GetMissingSubejcts(subjectRequest.tableName, studentId);

                var dataBaseAndScanUpdates = new DataBaseAndScanUpdates(studentId, neededSubjects, missingSubjects);

                await signalRMessages.AddAsync(
                new SignalRMessage
                {
                    UserId = studentId,
                    Target = ProjectConsts.SignalRTarget,
                    Arguments = new object[] { dataBaseAndScanUpdates }
                });

            }

            var classesTable = CloudTableUtilities.OpenTable(ProjectConsts.classesTableName);
            var classesEntities = CloudTableUtilities.getTableEntityAsync<ClassesTable>(classesTable).Result;

            foreach (var classEntity in classesEntities)
            {

                if (classEntity.subjectsTableName == subjectRequest.tableName)
                {
                    classEntity.LastTeacherUpdate = DateTime.Now;
                    await CloudTableUtilities.AddTableEntity(classesTable, classEntity);
                }

            }

            return JsonConvert.SerializeObject(operationResult);
        }

        #endregion

        #region Students and Parents functions


        [FunctionName("GetNeededSubjects")]
        public static async Task<string> GetNeededSubjects(
           [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestMessage request,
           ILogger log)
        {
            OperationResult operationResult = new OperationResult();
            SubjectRequest subjectRequest = await Utilities.ExtractContent<SubjectRequest>(request);
            var neededSubjects = SubjectsTableUtilities.GetNeededSubject(subjectRequest.tableName);
            operationResult.UpdateData(neededSubjects);
            return JsonConvert.SerializeObject(operationResult);
        }


        [FunctionName("GetMissingSubjects")]
        public static async Task<string> GetMissingSubjects(
           [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestMessage request,
           ILogger log)
        {
            OperationResult operationResult = new OperationResult();
            SubjectRequest subjectRequest = await Utilities.ExtractContent<SubjectRequest>(request);
            var missingSubjects = SubjectsTableUtilities.GetMissingSubejcts(subjectRequest.tableName, subjectRequest.userId);
            operationResult.UpdateData(missingSubjects);
            return JsonConvert.SerializeObject(operationResult);
        }


        [FunctionName("GetAllSubjects")]
        public static async Task<string> GetAllSubjects(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestMessage request, ILogger log)
        {
            {
                OperationResult operationResult = new OperationResult();
                SubjectRequest subjectsRequest = await Utilities.ExtractContent<SubjectRequest>(request);
                var allSubjects = SubjectsTableUtilities.GetAllSubjects(subjectsRequest.tableName);
                operationResult.UpdateData(allSubjects);
                return JsonConvert.SerializeObject(operationResult);
            }
        }

        [FunctionName("UpdateSubjectStickers")]
        public static async Task<string> UpdateSubjectSticker([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestMessage request,
        ILogger log)
        {
            OperationResult operationResult = new OperationResult();
            SubjectRequest subjectRequest = await Utilities.ExtractContent<SubjectRequest>(request);
            var subjectsTable = CloudTableUtilities.OpenTable(subjectRequest.tableName);
            var userStickers = CloudTableUtilities.getTableEntityAsync<SubjectsTable>(subjectsTable, subjectRequest.userId, SubjectsTableUtilities.Stickers).Result.First();
            var classSubjects = CloudTableUtilities.getTableEntityAsync<SubjectsTable>(subjectsTable, SubjectsTableUtilities.ClassSubjects).Result.First();

            var subjectsNamesDict = typeof(SubjectsTable).GetProperties().Where(x => x.Name.Contains(SubjectsTableUtilities.SubjectPropertyPrefix) && x.GetValue(classSubjects, null) != null).ToDictionary(prop => prop.GetValue(classSubjects, null).ToString(), prop => prop.Name);

            typeof(SubjectsTable).GetProperty(subjectsNamesDict.GetValueOrDefault(subjectRequest.subjectName)).SetValue(userStickers, subjectRequest.stickerId);
            await CloudTableUtilities.AddTableEntity(subjectsTable, userStickers);
            return JsonConvert.SerializeObject(operationResult);
        }

        [FunctionName("SendScanOperation")]
        public static async Task<string> SendScanOperation([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestMessage request, ILogger log)
        {
            OperationResult operationResult = new OperationResult();
            SubjectRequest subjectRequest = await Utilities.ExtractContent<SubjectRequest>(request);
            var operation = JsonConvert.SerializeObject(new { command = ProjectConsts.scanOperaion });
            await IotDeviceFunctions.SendCloudToDeviceMessageAsync(operation, subjectRequest.userId);
            return JsonConvert.SerializeObject(operationResult);
        }

        #endregion

    }
}
