using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
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
    public class ClassesTableFunctions
    {
        public static DateTime defaultDateTime = new DateTime(2000, 1, 01, 0, 0, 0).ToUniversalTime();


        [FunctionName("AddNewClass")]
        public static async Task<string> AddNewClass(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestMessage request,
        [Table("ClassesTable")] CloudTable classesTable, ILogger log)
        {
            List<string> teacherClassesIds;
            OperationResult result = new OperationResult();

            SchoolClass newClassInfo = await Utilities.ExtractContent<SchoolClass>(request);
            var usersTable = CloudTableUtilities.OpenTable(ProjectConsts.UsersTableName);
            var teacherEntity = CloudTableUtilities.getTableEntityAsync<UsersTable>(usersTable, newClassInfo.teacherId).Result.First();
            TableQuerySegment<ClassesTable> teacherClasses = await CloudTableUtilities.getTableEntityAsync<ClassesTable>(classesTable, teacherEntity.RowKey, newClassInfo.classId);

            if (!teacherClasses.Any())
            {
                string subjectsTableName = $"{teacherEntity.RowKey}{newClassInfo.classId}";
                SubjectsTableUtilities.CreateClassTable(subjectsTableName);
                var newClassEntity = new ClassesTable { subjectsTableName = newClassInfo.ToString() };
                newClassEntity.LastTeacherUpdate = defaultDateTime;
                TableOperation insertOperation = TableOperation.InsertOrReplace(newClassEntity);
                newClassEntity.PartitionKey = teacherEntity.RowKey;
                newClassEntity.RowKey = newClassInfo.classId;
                newClassEntity.subjectsTableName = subjectsTableName;
                await classesTable.ExecuteAsync(insertOperation);
                result.UpdateData(newClassEntity.subjectsTableName);

                if (teacherEntity.ClassId != null)
                {
                    teacherClassesIds = teacherEntity.ClassId.Split(ProjectConsts.delimiter).ToList();
                }
                else
                {
                    teacherClassesIds = new List<string>();
                }

                teacherClassesIds.Add(newClassInfo.classId);
                teacherEntity.ClassId = string.Join(ProjectConsts.delimiter, teacherClassesIds);
                await CloudTableUtilities.AddTableEntity<UsersTable>(usersTable, teacherEntity);
            }
            else
            {
                result.UpdateFailure(ErrorMessages.classAlreadyExist);
            }

            return JsonConvert.SerializeObject(result);
        }
    }
}


