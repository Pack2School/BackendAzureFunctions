using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Table;
using System.Text;
using Pack2SchoolFunction.Templates;
using Pack2SchoolFunction.Tables;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Microsoft.Azure.EventHubs.Processor;
using System.Linq;
using Pack2SchoolFunctions;
using Pack2SchoolFunction;

namespace Pack2SchoolFunctions
{
    public static class SubjectsTableUtilities
    {

        private static readonly string classSubjectsPartitionKey = "0";
        private static readonly string NecessityPartitionKey = "1";
        private static readonly string NecessityRowKey = "Necessity";
        private static readonly string SubjectRowKey = "Subjects";
        private static readonly string SubjectPropertyPrefix = "Subject";
        private static readonly string DeviceId = "deviceId";

        const string StickerRowKey = "stickers";
        const string BroughtRowKey = "brought";
        const string NeededSubject = "true";
        const string NotNeededSubject = "false";
        const string InsideTheBag = "true";
        const string NotInsideTheBag = "false";
      


        internal static async Task AddStuentToClassTableAsync(string subjectsTableName, UserRequest newUserRequest, OperationResult response)
        {
            var table = CloudTableUtilities.OpenTable(subjectsTableName);
            SubjectsTable newStudentNecessity = new SubjectsTable();
            SubjectsTable newStudentSubjects = new SubjectsTable();

            if (table == null)
            {
                response.UpdateFailure(ErrorMessages.subjectTableNotExist);
                response.requestSucceeded = false;
                return;
            }
        
            var subjectsEntity = CloudTableUtilities.getTableEntityAsync<SubjectsTable>(table, classSubjectsPartitionKey).Result.First();
            var subjectNames = typeof(SubjectsTable).GetProperties().Where(x => x.Name.Contains(SubjectPropertyPrefix) && x.GetValue(subjectsEntity, null) !=  null);

            foreach (var subject in subjectNames)
            {
                subject.SetValue(newStudentNecessity, NotInsideTheBag);
            }

            await CloudTableUtilities.AddTableEntity(table, newStudentNecessity, newUserRequest.userId, NecessityRowKey);
            await CloudTableUtilities.AddTableEntity(table, newStudentSubjects, newUserRequest.userId, SubjectRowKey);
        }

        public static void RenameSubject(SubjectRequest editSubjectRequest, SubjectsTable subjectsNames, SubjectsTable subjectsNecessity, OperationResult operationResult)
        {
            var subject = typeof(SubjectsTable).GetProperties().Where(x => x.GetValue(subjectsNames, null)?.ToString() == editSubjectRequest.subjectName);

            if (!subject.Any())
            {
                operationResult.UpdateFailure(ErrorMessages.subjetNotFound);
            }
            else
            {
                subject.First().SetValue(subjectsNames, editSubjectRequest.newSubjectName);
            }
        }

        public static void AddNewSubject(SubjectRequest addSubjectRequest, SubjectsTable subjectsNames, SubjectsTable subjectsNecessity, OperationResult operationResult)
        {
            var subjectsName = typeof(SubjectsTable).GetProperties().Where(x => x.Name.Contains(SubjectPropertyPrefix) && x.GetValue(subjectsNames, null) == null);

            if (!subjectsName.Any())
            {
                operationResult.UpdateFailure(ErrorMessages.noSubjectsAvailable);
            }
            else
            {
                subjectsName.First().SetValue(subjectsNames,addSubjectRequest.subjectName);
                subjectsName.First().SetValue(subjectsNecessity, "false");
            }
        }

        public static void DeleteSubject(SubjectRequest editSubjectRequest, SubjectsTable subjectsNames, SubjectsTable subjectsNecessity, OperationResult operationResult)
        {
            var subjectsName = typeof(SubjectsTable).GetProperties().Where(x => x.GetValue(subjectsNames, null)?.ToString() == editSubjectRequest.subjectName);

            if (!subjectsName.Any())
            {
                operationResult.UpdateFailure(ErrorMessages.subjetNotFound);
            }
            else
            {
                var subject = subjectsName.First();
                subject.SetValue(subjectsNames, null);
                subject.SetValue(subjectsNecessity, null);
            }
        }

        public static void UpdateSubjectNecessity(SubjectsTable SubjectNames, SubjectsTable Necessity, SubjectRequest subjectRequest)
        {
            var subjectName = subjectRequest.subjectName;
            var operationReslut = new OperationResult();
            var propertiesDict = typeof(SubjectsTable).GetProperties().ToDictionary(prop => prop.Name, prop => prop.GetValue(SubjectNames, null));
            var propertyName = Utilities.KeyByValue(propertiesDict, subjectName);
            typeof(SubjectsTable).GetProperty(propertyName).SetValue(Necessity, subjectRequest.neededForTomorrow);
        }

        public static List<string> GetNecessitySubjects(SubjectsTable subjects, SubjectsTable subjectsNecessity)
        {
            var subjectsNameDict = typeof(SubjectsTable).GetProperties().ToDictionary(prop => prop.Name, prop => prop.GetValue(subjects, null));
            var properties = typeof(SubjectsTable).GetProperties().Where(x => x.Name.Contains(SubjectPropertyPrefix) && x.GetValue(subjectsNecessity, null)?.ToString() == "true");
            var necessitysubjects = properties.Select(x => subjectsNameDict[x.Name.ToString()].ToString()).ToList();
            return necessitysubjects;  
        }

        internal static void ResetNecessities(SubjectsTable subjects)
        {
            List<string> neededSubjects = new List<string>();

            if (subjects.SubjectA == "yes")
            {
                neededSubjects.Add(subjects.SubjectA);
            }
            else if (subjects.SubjectB == "yes")
            {
                neededSubjects.Add(subjects.SubjectB);
            }
            else if (subjects.SubjectC == null)
            {
                neededSubjects.Add(subjects.SubjectC);
            }
            else if (subjects.SubjectD == "yes")
            {
                neededSubjects.Add(subjects.SubjectD);
            }
            else if (subjects.SubjectE == "yes")
            {
                neededSubjects.Add(subjects.SubjectE);
            }
            else if (subjects.SubjectF == "yes")
            {
                neededSubjects.Add(subjects.SubjectF);
            }
        }

        internal static List<string> GetMissingTable(SubjectsTable subjectsNames, SubjectsTable subjectsNecessity, SubjectsTable studentSubjects)
        {
            List<string> missingSubjects = new List<string>();

            var subjectsNamesDict = typeof(SubjectsTable).GetProperties().Where(x => x.Name.Contains(SubjectPropertyPrefix) && x.GetValue(subjectsNames, null)!=null).ToDictionary(prop => prop.Name, prop => prop.GetValue(subjectsNames, null).ToString());
            var subjectsNecessityDict = typeof(SubjectsTable).GetProperties().Where(x => x.Name.Contains(SubjectPropertyPrefix) && x.GetValue(subjectsNames, null) != null).ToDictionary(prop => prop.Name, prop => prop.GetValue(subjectsNecessity, null)?.ToString());
            var studentSubjectsDict = typeof(SubjectsTable).GetProperties().Where(x => x.Name.Contains(SubjectPropertyPrefix) && x.GetValue(subjectsNames, null) != null).ToDictionary(prop => prop.Name, prop => prop.GetValue(studentSubjects, null)?.ToString());

            foreach (var subjectNecessity in subjectsNecessityDict)
            {
                if (subjectNecessity.Value == NeededSubject && studentSubjectsDict.GetValueOrDefault(subjectNecessity.Key) == NotInsideTheBag)
                {
                    
                    missingSubjects.Add(subjectsNamesDict.GetValueOrDefault(subjectNecessity.Key));
                }
            }

            return missingSubjects;
        }

        public static async void CreateClassTable(string tableName)
        {
            var table = await CloudTableUtilities.CreateTableAsync(tableName);
            var subjectNames = new SubjectsTable();
            TableOperation insertOperation = TableOperation.InsertOrReplace(subjectNames);
            subjectNames.PartitionKey = "0";
            subjectNames.RowKey = "0";
            await table.ExecuteAsync(insertOperation);
            var subjectNecessity = new SubjectsTable();
            insertOperation = TableOperation.InsertOrReplace(subjectNecessity);
            subjectNecessity.PartitionKey = "1";
            subjectNecessity.RowKey = "1";
            await table.ExecuteAsync(insertOperation);

        }

        internal static void UpdateSubjectState(CloudTable subjectsTable, SubjectsTable userSubject, SubjectsTable subjectsState, Dictionary<string, string> newStates)
        {
            var userSubjects = typeof(SubjectsTable).GetProperties().ToDictionary(prop => prop.GetValue(userSubject, null), prop => prop.Name);

            foreach(var subject in userSubjects)
            {
                typeof(SubjectsTable).GetProperty(subject.Value).SetValue(subjectsState, newStates.GetValueOrDefault(subject.Key.ToString()));
            }
        }

        internal static void ResetProperties(SubjectsTable necessityRow)
        {
            var subjects = typeof(SubjectsTable).GetProperties().Where(x => x.Name.Contains(SubjectPropertyPrefix));
            foreach (var subject in subjects)
            {
                subject.SetValue(necessityRow, NotInsideTheBag);
            }
        }
    }
}
