using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
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
    public class ScheduleTableFunctions
    {
        [FunctionName("SetClassSchedule")]
        public static async Task<string> SetClassSchedule(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestMessage request,
        [Table("ClassesTable")] CloudTable classesTable, ILogger log)
        {
            OperationResult result = new OperationResult();
            CloudTable scheduleTable;
            ScheduleSetter newClassInfo = await Utilities.ExtractContent<ScheduleSetter>(request);
            var classesEntities = CloudTableUtilities.getTableEntityAsync<ClassesTable>(classesTable).Result;

            foreach (var classEntity in classesEntities)
            {

                if (classEntity.subjectsTableName == newClassInfo.className)
                {
                    if (!CloudTableUtilities.TableExist($"{classEntity.subjectsTableName}Schedule"))
                    {
                        scheduleTable = await CloudTableUtilities.CreateTableAsync($"{classEntity.subjectsTableName}Schedule");
                        classEntity.ScheduleTableName = $"{classEntity.subjectsTableName}Schedule";

                    }
                    else
                    {
                        scheduleTable = CloudTableUtilities.OpenTable($"{classEntity.subjectsTableName}Schedule");
                    }

                    var daysInfo1 = typeof(ScheduleSetter).GetProperties().ToList();
                    var daysInfo = typeof(ScheduleSetter).GetProperties().Where(x => x.Name != "className").ToList();
                    foreach (var dayinfo in daysInfo)
                    {
                        var dayEnity = new ScheduleTable();
                        var tableColumns = typeof(ScheduleTable).GetProperties().Where(x => x.Name.Contains(SubjectsTableUtilities.SubjectPropertyPrefix)).ToList();
                        var subjectsList = (List<string>)dayinfo.GetValue(newClassInfo);

                        foreach (var subject in subjectsList)
                        {
                            var col = tableColumns[0];
                            tableColumns.RemoveAt(0);
                            col.SetValue(dayEnity, subject);
                        }

                        dayEnity.PartitionKey = dayinfo.Name;
                        dayEnity.RowKey = dayinfo.Name;
                        await CloudTableUtilities.AddTableEntity(scheduleTable, dayEnity);
                    }

                    classEntity.LastTeacherUpdate = new DateTime(2000, 1, 01, 0, 0, 0).ToUniversalTime();
                    await CloudTableUtilities.AddTableEntity(classesTable, classEntity);
                }
            }

            return JsonConvert.SerializeObject(result);
        }

        [FunctionName("UpadteNecessityBySchedule")]
        public static async Task RunAsync([TimerTrigger("0 35 08 * * *")]TimerInfo myTimer, [SignalR(HubName = "Pack2SchoolSignalR1")] IAsyncCollector<SignalRMessage> signalRMessages, ILogger log)
        {
            var TodayDateTime = DateTime.Today;
            var classesTable = CloudTableUtilities.OpenTable(ProjectConsts.classesTableName);
            var classesEntities = CloudTableUtilities.getTableEntityAsync<ClassesTable>(classesTable).Result;

            foreach (var classEntity in classesEntities)
            {
                if (classEntity.ScheduleTableName == null)
                {
                    continue;
                }

                if (classEntity.LastTeacherUpdate.Date != TodayDateTime)
                {
                    var day = System.DateTime.Now.DayOfWeek.ToString();
                    var scheduleTable = CloudTableUtilities.OpenTable(classEntity.ScheduleTableName);
                    var subjectsEntity = CloudTableUtilities.getTableEntityAsync<ScheduleTable>(scheduleTable, day).Result.First();
                    var neededSubjects = typeof(ScheduleTable).GetProperties().Where(propInfo => propInfo.Name.Contains(SubjectsTableUtilities.SubjectPropertyPrefix) && propInfo.GetValue(subjectsEntity) != null)
                     .Select(propInfo => propInfo.GetValue(subjectsEntity)?.ToString()).ToList();

                    var subjectsTable = CloudTableUtilities.OpenTable(classEntity.subjectsTableName);
                    var subjectsNames = CloudTableUtilities.getTableEntityAsync<SubjectsTable>(subjectsTable, SubjectsTableUtilities.ClassSubjects).Result.First();
                    var subjectsNecessity = CloudTableUtilities.getTableEntityAsync<SubjectsTable>(subjectsTable, SubjectsTableUtilities.Necessity).Result.First();
                    SubjectsTableUtilities.UpdateSubjectNecessity(subjectsNames, subjectsNecessity, neededSubjects);
                    await CloudTableUtilities.AddTableEntity(subjectsTable, subjectsNecessity);
                    neededSubjects = SubjectsTableUtilities.GetNeededSubject(classEntity.subjectsTableName);
                    var studentsIds = SubjectsTableUtilities.GetAllStudentsIds(classEntity.subjectsTableName);

                    foreach (var studentId in studentsIds)
                    {
                        var missingSubjects = SubjectsTableUtilities.GetMissingSubejcts(classEntity.subjectsTableName, studentId);

                        var dataBaseAndScanUpdates = new DataBaseAndScanUpdates(studentId, neededSubjects, missingSubjects);

                        await signalRMessages.AddAsync(
                        new SignalRMessage
                        {
                            UserId = studentId,
                            Target = ProjectConsts.SignalRTarget,
                            Arguments = new object[] { dataBaseAndScanUpdates }
                        });
                    }
                }
            }
        }
    }
}
