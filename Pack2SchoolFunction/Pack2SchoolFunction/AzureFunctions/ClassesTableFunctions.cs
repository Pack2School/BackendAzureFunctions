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
using Microsoft.Azure.Amqp.Framing;

namespace Pack2SchoolFunctions
{
    public class ClassesTableFunctions
    {
        /// <summary>
        /// Add new class to the classes table and creates subjects table for this class
        /// </summary>
        /// <param name="request">contains details of the class</param>
        /// <param name="classesTable"></param>
        /// <param name="log">log</param>
        /// <returns></returns>
        [FunctionName("AddNewClass")]
        public static async Task<OperationResult> AddNewClass(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestMessage request,
        [Table("ClassesTable")] CloudTable classesTable, ILogger log)
        {
            OperationResult result = new OperationResult();
            SchoolClass newClassInfo = await Utilities.ExtractContent<SchoolClass>(request);
            var usersTable = CloudTableUtilities.OpenTable(ProjectConsts.UsersTableName);
            var teacherEntity = CloudTableUtilities.getTableEntityAsync<UsersTable>(usersTable, newClassInfo.teacherId).Result.First();
            TableQuerySegment<ClassesTable> classesResult = await CloudTableUtilities.getTableEntityAsync<ClassesTable>(classesTable, teacherEntity.RowKey, newClassInfo.grade);

            if (!classesResult.Any())
            {
                SubjectsTableUtilities.CreateClassTable($"{teacherEntity.RowKey}{newClassInfo.grade}");
                var newClassEntity = new ClassesTable { subjectsTableName = newClassInfo.ToString() };
                TableOperation insertOperation = TableOperation.InsertOrReplace(newClassEntity);
                newClassEntity.PartitionKey = teacherEntity.RowKey;
                newClassEntity.RowKey = newClassInfo.grade;
                newClassEntity.subjectsTableName = $"{teacherEntity.RowKey}{newClassInfo.grade}";
                await classesTable.ExecuteAsync(insertOperation);
                result.UpdateData(newClassEntity.subjectsTableName);
                var updatedTeacherGrades = teacherEntity.Grades.Split(ProjectConsts.delimiter).ToList();
                updatedTeacherGrades.Add(newClassInfo.grade);
                teacherEntity.Grades = string.Join(ProjectConsts.delimiter, updatedTeacherGrades);
                await CloudTableUtilities.AddTableEntity<UsersTable>(usersTable, teacherEntity, teacherEntity.PartitionKey, teacherEntity.RowKey);
            }
            else
            {
                result.UpdateFailure(ErrorMessages.classAlreadyExist);
            }

            return result;
        }
    }
}
