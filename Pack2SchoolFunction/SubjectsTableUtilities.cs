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
using System.Reflection.Metadata.Ecma335;
using Pack2SchoolFunction;

namespace Pack2SchoolFunctions
{
    public static class SubjectsTableUtilities
    {
        const string StickerRowKey = "stickers";
        const string BroughtRowKey = "brought";
        const string SubjectPropertyPrefix = "Subject";
        const string NeededSubject = "true";
        const string NotNeededSubject = "false";
        const string InsideTheBag = "true";
        const string NotInsideTheBag = "false";
      

        internal static async Task AddStuentToClassTableAsync(UserRequest newUserRequest, Response response)
        {
            var table = CloudTableOperation.OpenTable(newUserRequest.subjectsTableName);

            if (table == null)
            {
                response.errorMessage = Response.subjectTableNotExist;
                response.requestSucceeded = false;
                return;
            }

            var subjects = CloudTableOperation.getTableEntityAsync<SubjectsTable>(table, "0").Result.Results.First();
            SubjectsTable newStudent = new SubjectsTable();

            if (subjects.SubjectA != null)
            {
                newStudent.SubjectA = "false";
            }
            if (subjects.SubjectB != null)
            {
                newStudent.SubjectB = "false";
            }
            if (subjects.SubjectC != null)
            {
                newStudent.SubjectC = "false";
            }
            if (subjects.SubjectD != null)
            {
                newStudent.SubjectD = "false";
            }
            if (subjects.SubjectE != null)
            {
                newStudent.SubjectE = "false";
            }
            if (subjects.SubjectF != null)
            {
                newStudent.SubjectF = "false";
            }

            CloudTableOperation.AddTableEntity(table, newStudent, newUserRequest.userId, newUserRequest.userName);
        }

        public static void addNewSubject(SubjectRequest addSubjectRequest, SubjectsTable subjectsNames, SubjectsTable subjectsNecessity)
        {
            if (subjectsNames.SubjectA == null)
            {
                subjectsNames.SubjectA = addSubjectRequest.subject;
                subjectsNecessity.SubjectA = " false";
            }
            else if (subjectsNames.SubjectB == null)
            {
                subjectsNames.SubjectB = addSubjectRequest.subject;
                subjectsNecessity.SubjectB = " false";
            }
            else if (subjectsNames.SubjectC == null)
            {
                subjectsNames.SubjectC = addSubjectRequest.subject;
                subjectsNecessity.SubjectC = " false";
            }
            else if (subjectsNames.SubjectD == null)
            {
                subjectsNames.SubjectD = addSubjectRequest.subject;
                subjectsNecessity.SubjectD = " false";
            }
            else if (subjectsNames.SubjectE == null)
            {
                subjectsNames.SubjectE = addSubjectRequest.subject;
                subjectsNecessity.SubjectE = " false";
            }
            else if (subjectsNames.SubjectF == null)
            {
                subjectsNames.SubjectF = addSubjectRequest.subject;
                subjectsNecessity.SubjectF = " false";
            }
        }

        public static void UpdateSubjectNecessity(SubjectsTable SubjectNames, SubjectsTable Necessity, SubjectRequest subjectRequest)
        {
            var subjectName = subjectRequest.subject;
            var propertiesDict = typeof(SubjectsTable).GetProperties().ToDictionary(prop => prop.Name, prop => prop.GetValue(SubjectNames, null));
            var propertyName = Utilities.KeyByValue(propertiesDict, subjectName);
            typeof(SubjectsTable).GetProperty(propertyName).SetValue(Necessity, subjectRequest.needed);
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

            var subjectsNamesDict = typeof(SubjectsTable).GetProperties().ToDictionary(prop => prop.Name, prop => prop.GetValue(subjectsNames, null).ToString());
            var subjectsNecessityDict = typeof(SubjectsTable).GetProperties().ToDictionary(prop => prop.Name, prop => prop.GetValue(subjectsNecessity, null).ToString());
            var studentSubjectsDict = typeof(SubjectsTable).GetProperties().ToDictionary(prop => prop.Name, prop => prop.GetValue(subjectsNecessity, null).ToString());

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
            var table = await CloudTableOperation.CreateTableAsync(tableName);
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
    }
}
