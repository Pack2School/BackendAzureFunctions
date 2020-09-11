using Microsoft.WindowsAzure.Storage.Table;
using Pack2SchoolFunction.Templates;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pack2SchoolFunctions
{
    public static class SubjectsTableUtilities
    {
     
        public static readonly string ClassSubjects = "0";

        public static readonly string Necessity= "1";

        public static readonly string SubjectPropertyPrefix = "Subject";

        public const string Stickers = "Stickers";

        const string IsInsideTheBag = "IsInsideTheBag";

        const string NeededSubject = "true";

        const string NotNeededSubject = "false";

        const string InsideTheBag = "true";

        const string NotInsideTheBag = "false";

        public static async Task AddStuentToClassTableAsync(string subjectsTableName, UserRequest newUserRequest, OperationResult response)
        {
            var table = CloudTableUtilities.OpenTable(subjectsTableName);

            if (table == null)
            {
                response.UpdateFailure(ErrorMessages.subjectTableNotExist);
                response.requestSucceeded = false;
                return;
            }

            SubjectsTable newIsInsideTheBagEntity = new SubjectsTable();
            SubjectsTable newStickersEntity = new SubjectsTable();
            var classSubjectsEntity = CloudTableUtilities.getTableEntityAsync<SubjectsTable>(table, ClassSubjects).Result.First();
            var classSubjectsProperties = typeof(SubjectsTable).GetProperties().Where(x => x.Name.Contains(SubjectPropertyPrefix) && x.GetValue(classSubjectsEntity, null) !=  null);

            foreach (var classSubjectProperty in classSubjectsProperties)
            {
                classSubjectProperty.SetValue(newIsInsideTheBagEntity, NotInsideTheBag);
            }

            await CloudTableUtilities.AddTableEntity(table, newIsInsideTheBagEntity, newUserRequest.userId, IsInsideTheBag);
            await CloudTableUtilities.AddTableEntity(table, newStickersEntity, newUserRequest.userId, Stickers);
        }

        public static List<string> GetNeededSubject(string tableName)
        {
            var subjectsTable = CloudTableUtilities.OpenTable(tableName);
            var classSubjectsNamesEntity = CloudTableUtilities.getTableEntityAsync<SubjectsTable>(subjectsTable, ClassSubjects).Result.First();
            var classSubjectsNecessityEntity = CloudTableUtilities.getTableEntityAsync<SubjectsTable>(subjectsTable, Necessity).Result.First();
            var subjectsNamesValuesMapping = typeof(SubjectsTable).GetProperties().ToDictionary(prop => prop.Name, prop => prop.GetValue(classSubjectsNamesEntity, null));
            var neededSubjectsAsProperties = typeof(SubjectsTable).GetProperties().Where(x => x.Name.Contains(SubjectPropertyPrefix) && x.GetValue(classSubjectsNecessityEntity, null)?.ToString() == "true");
            var neededSubjects = neededSubjectsAsProperties.Select(x => subjectsNamesValuesMapping[x.Name.ToString()].ToString()).ToList();
            return neededSubjects;
        }

        public static List<string> GetAllSubjects(string tableName)
        { 
            var subjectsTable = CloudTableUtilities.OpenTable(tableName);
            var classSubjectsNamesEntity = CloudTableUtilities.getTableEntityAsync<SubjectsTable>(subjectsTable, ClassSubjects).Result.First();
            return typeof(SubjectsTable).GetProperties().Where(propInfo => propInfo.Name.Contains(SubjectPropertyPrefix) && propInfo.GetValue(classSubjectsNamesEntity) != null)
                .Select(propInfo => propInfo.GetValue(classSubjectsNamesEntity)?.ToString()).ToList();
        }

        public static async Task RenameSubjectAsync(SubjectRequest editSubjectRequest, SubjectsTable subjectsNames, string scheduleTableName, SubjectsTable subjectsNecessity, OperationResult operationResult)
        {
            var subjects = typeof(SubjectsTable).GetProperties().Where(x => x.GetValue(subjectsNames, null)?.ToString() == editSubjectRequest.subjectName);

            if (!subjects.Any())
            {
                operationResult.UpdateFailure(ErrorMessages.subjetNotFound);
            }
            else
            {
                subjects.First().SetValue(subjectsNames, editSubjectRequest.newSubjectName);
            }

            if (scheduleTableName != null)
            {
                var scheduleTable = CloudTableUtilities.OpenTable(scheduleTableName);
                var daysEntities = CloudTableUtilities.getTableEntityAsync<ScheduleTable>(scheduleTable).Result;

                foreach ( var dayEntity in daysEntities)
                {
                    var daySubjects = typeof(ScheduleTable).GetProperties().Where(x => x.Name.Contains(SubjectPropertyPrefix) && x.GetValue(dayEntity, null) != null).ToList();
                    
                    foreach (var subjectProperty in daySubjects)
                    {
                        if (subjectProperty.GetValue(dayEntity, null).ToString() == editSubjectRequest.subjectName)
                        {
                            subjectProperty.SetValue(dayEntity, editSubjectRequest.newSubjectName);
                        }
                    }

                    await CloudTableUtilities.AddTableEntity(scheduleTable, dayEntity);
                }
            }
        }

        public static void AddNewSubject(SubjectRequest addSubjectRequest, SubjectsTable classSubjectsNamesEntity, SubjectsTable classSubjectsNecessityEntity, OperationResult operationResult)
        {
            var subjectsWithoutValues = typeof(SubjectsTable).GetProperties().Where(x => x.Name.Contains(SubjectPropertyPrefix) && x.GetValue(classSubjectsNamesEntity, null) == null);

            if (!subjectsWithoutValues.Any())
            {
                operationResult.UpdateFailure(ErrorMessages.noSubjectsAvailable);
            }
            else
            {
                var subject = subjectsWithoutValues.First();
                subject.SetValue(classSubjectsNamesEntity,addSubjectRequest.subjectName);
                subject.SetValue(classSubjectsNecessityEntity, NotNeededSubject);
            }
        }

        public static async Task DeleteSubjectAsync(SubjectRequest editSubjectRequest, SubjectsTable classSubjectsNamesEntity, string scheduleTableName, SubjectsTable classSubjectsNecessityEntity, OperationResult operationResult)
        {
            var subjectsName = typeof(SubjectsTable).GetProperties().Where(x => x.GetValue(classSubjectsNamesEntity, null)?.ToString() == editSubjectRequest.subjectName);

            if (!subjectsName.Any())
            {
                operationResult.UpdateFailure(ErrorMessages.subjetNotFound);
            }
            else
            {
                var subject = subjectsName.First();
                subject.SetValue(classSubjectsNamesEntity, null);
                subject.SetValue(classSubjectsNecessityEntity, null);
            }

            if (scheduleTableName != null)
            {
                var scheduleTable = CloudTableUtilities.OpenTable(scheduleTableName);
                var daysEntities = CloudTableUtilities.getTableEntityAsync<ScheduleTable>(scheduleTable).Result;

                foreach (var dayEntity in daysEntities)
                {
                    var daySubjects = typeof(ScheduleTable).GetProperties().Where(x => x.Name.Contains(SubjectPropertyPrefix) && x.GetValue(dayEntity, null) != null);

                    foreach (var subjectProperty in daySubjects)
                    {
                        if (subjectProperty.GetValue(dayEntity, null).ToString() == editSubjectRequest.subjectName)
                        {
                            subjectProperty.SetValue(dayEntity, null);
                        }
                    }

                    await CloudTableUtilities.AddTableEntity(scheduleTable, dayEntity);
                }
            }
        }

        public static List<string> GetMissingSubejcts(string tableName, string userId)
        {
            var subjectsTable = CloudTableUtilities.OpenTable(tableName);
            var classSubjectsNamesEntity = CloudTableUtilities.getTableEntityAsync<SubjectsTable>(subjectsTable, ClassSubjects).Result.First();
            var classSubjectsNecessityEntity = CloudTableUtilities.getTableEntityAsync<SubjectsTable>(subjectsTable, Necessity).Result.First();
            var studentIsInsideTheBagEntity = CloudTableUtilities.getTableEntityAsync<SubjectsTable>(subjectsTable, userId, IsInsideTheBag).Result.First();

            List<string> missingSubjects = new List<string>();


            var subjectsNameToValueMapping = typeof(SubjectsTable).GetProperties().Where(x => x.Name.Contains(SubjectPropertyPrefix) && x.GetValue(classSubjectsNamesEntity, null) != null).ToDictionary(prop => prop.Name, prop => prop.GetValue(classSubjectsNamesEntity, null).ToString());
            var subjectsNameToNecessityMapping = typeof(SubjectsTable).GetProperties().Where(x => x.Name.Contains(SubjectPropertyPrefix) && x.GetValue(classSubjectsNamesEntity, null) != null).ToDictionary(prop => prop.Name, prop => prop.GetValue(classSubjectsNecessityEntity, null)?.ToString());
            var subejctsNamesToPresenceInbagMapping = typeof(SubjectsTable).GetProperties().Where(x => x.Name.Contains(SubjectPropertyPrefix) && x.GetValue(classSubjectsNamesEntity, null) != null).ToDictionary(prop => prop.Name, prop => prop.GetValue(studentIsInsideTheBagEntity, null)?.ToString());

            foreach (var subjectNecessity in subjectsNameToNecessityMapping)
            {
                if (subjectNecessity.Value == NeededSubject && subejctsNamesToPresenceInbagMapping.GetValueOrDefault(subjectNecessity.Key) == NotInsideTheBag)
                {

                    missingSubjects.Add(subjectsNameToValueMapping.GetValueOrDefault(subjectNecessity.Key));
                }
            }

            return missingSubjects;
        }

        public static void UpdateSubjectNecessity(SubjectsTable classSubjectsNamesEntity, SubjectsTable classSubjectsNecessityEntity, List<string> neededSubjects)
        {
            var operationReslut = new OperationResult();

            var subjectsNameToValueMapping = typeof(SubjectsTable).GetProperties().Where(x => x.Name.Contains(SubjectPropertyPrefix) && x.GetValue(classSubjectsNamesEntity, null) != null).ToDictionary(prop => prop.Name, prop => prop.GetValue(classSubjectsNamesEntity, null).ToString());

            foreach (var subjectMappingPair in subjectsNameToValueMapping)
            {
                if (neededSubjects.Contains(subjectMappingPair.Value))
                {
                    typeof(SubjectsTable).GetProperty(subjectMappingPair.Key).SetValue(classSubjectsNecessityEntity, NeededSubject);
                }
                else
                {
                    typeof(SubjectsTable).GetProperty(subjectMappingPair.Key).SetValue(classSubjectsNecessityEntity, NotNeededSubject);
                }
            }

        }

        public static async void CreateClassTable(string tableName)
        {
            var subjectsTable = await CloudTableUtilities.CreateTableAsync(tableName);
            var classSubjectsNamesEntity = new SubjectsTable();
            TableOperation insertOperation = TableOperation.InsertOrReplace(classSubjectsNamesEntity);
            classSubjectsNamesEntity.PartitionKey = ClassSubjects;
            classSubjectsNamesEntity.RowKey = ClassSubjects;
            await subjectsTable.ExecuteAsync(insertOperation);
            var classSubjectsNecessityEntity = new SubjectsTable();
            insertOperation = TableOperation.InsertOrReplace(classSubjectsNecessityEntity);
            classSubjectsNecessityEntity.PartitionKey = Necessity;
            classSubjectsNecessityEntity.RowKey = Necessity;
            await subjectsTable.ExecuteAsync(insertOperation);
        }

        public static async Task<List<List<string>>> UpdateBagContent(string studentId, List<string> subjectsInTheBag)
        {
            var usersTable = CloudTableUtilities.OpenTable(ProjectConsts.UsersTableName);
            var studentEntity = CloudTableUtilities.getTableEntityAsync<UsersTable>(usersTable, studentId).Result.First();
            var subjectsTableName = UsersTableUtilities.GetSubjectsTableNamesForStudent(studentEntity).First();
            var subjectsTable = CloudTableUtilities.OpenTable(subjectsTableName);
            var studentIsInsideTheBagEntity = CloudTableUtilities.getTableEntityAsync<SubjectsTable>(subjectsTable, studentId, IsInsideTheBag).Result.First();
            var studentStickersEntity = CloudTableUtilities.getTableEntityAsync<SubjectsTable>(subjectsTable, studentId, Stickers).Result.First();
            var subjectsNamesValuesMapping = typeof(SubjectsTable).GetProperties().Where(x => x.Name.Contains(SubjectPropertyPrefix) && x.GetValue(studentStickersEntity, null) != null).ToDictionary(prop => prop.Name, prop => prop.GetValue(studentStickersEntity, null)?.ToString());

            foreach (var mapping in subjectsNamesValuesMapping)
            {
                if (subjectsInTheBag.Contains(mapping.Value))
                {
                    typeof(SubjectsTable).GetProperty(mapping.Key).SetValue(studentIsInsideTheBagEntity, InsideTheBag);
                }
                else
                {
                    typeof(SubjectsTable).GetProperty(mapping.Key).SetValue(studentIsInsideTheBagEntity, NotInsideTheBag);
                }
            }

            await CloudTableUtilities.AddTableEntity(subjectsTable, studentIsInsideTheBagEntity);

            return new List<List<string>>() { GetExtraSubjects(subjectsTableName, studentId), GetMissingSubejcts(subjectsTableName, studentId) };
        }

        
        public static List<string> GetExtraSubjects(string tableName, string userId)
        {
            var subjectsTable = CloudTableUtilities.OpenTable(tableName);
            var classSubjectsNamesEntity = CloudTableUtilities.getTableEntityAsync<SubjectsTable>(subjectsTable, ClassSubjects).Result.First();
            var classSubjectsNecessityEntity = CloudTableUtilities.getTableEntityAsync<SubjectsTable>(subjectsTable, Necessity).Result.First();
            var studentIsInsideTheBagEntity = CloudTableUtilities.getTableEntityAsync<SubjectsTable>(subjectsTable, userId, IsInsideTheBag).Result.First();

            List<string> missingSubjects = new List<string>();


            var subjectsNamesValuesMapping = typeof(SubjectsTable).GetProperties().Where(x => x.Name.Contains(SubjectPropertyPrefix) && x.GetValue(classSubjectsNamesEntity, null) != null).ToDictionary(prop => prop.Name, prop => prop.GetValue(classSubjectsNamesEntity, null).ToString());
            var subjectsNamesNecessityMapping = typeof(SubjectsTable).GetProperties().Where(x => x.Name.Contains(SubjectPropertyPrefix) && x.GetValue(classSubjectsNamesEntity, null) != null).ToDictionary(prop => prop.Name, prop => prop.GetValue(classSubjectsNecessityEntity, null)?.ToString());
            var subejctsNamesToPresenceInbagMapping = typeof(SubjectsTable).GetProperties().Where(x => x.Name.Contains(SubjectPropertyPrefix) && x.GetValue(classSubjectsNamesEntity, null) != null).ToDictionary(prop => prop.Name, prop => prop.GetValue(studentIsInsideTheBagEntity, null)?.ToString());

            foreach (var subjectNecessity in subjectsNamesNecessityMapping)
            {
                if (subjectNecessity.Value == NotNeededSubject && subejctsNamesToPresenceInbagMapping.GetValueOrDefault(subjectNecessity.Key) == InsideTheBag)
                {

                    missingSubjects.Add(subjectsNamesValuesMapping.GetValueOrDefault(subjectNecessity.Key));
                }
            }

            return missingSubjects;

        }

        public static List<string> GetAllStudentsIds(string tableName)
        {
            var subjectsTable = CloudTableUtilities.OpenTable(tableName);
            var studentsEntity = CloudTableUtilities.getTableEntityAsync<SubjectsTable>(subjectsTable, null, IsInsideTheBag).Result;
            return studentsEntity.Select(x => x.PartitionKey).ToList();
        }
    }
}
