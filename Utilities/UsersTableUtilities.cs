using Microsoft.Azure.Documents;
using Microsoft.WindowsAzure.Storage.Table;
using Pack2SchoolFunction.Tables;
using Pack2SchoolFunction.Templates;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Pack2SchoolFunctions
{
    public static class UsersTableUtilities
    {
        public static bool ValidateUserNotExist(TableQuerySegment<UsersTable> usersResult, UserRequest newUserRequest, OperationResult response)
        {
            foreach (UsersTable user in usersResult.Results)
            {
                if (user.PartitionKey == newUserRequest.userId)
                {
                    response.UpdateFailure(ErrorMessages.UserExist);
                    return response.requestSucceeded;
                }
            }

            return response.requestSucceeded;
        }

        public static bool ValidateChildrenIdExist(TableQuerySegment<UsersTable> usersResult, UserRequest newUserRequest, OperationResult response)
        {
            var childerns = newUserRequest.childrenIds.Select(item => (string)item.Clone()).ToList();

            foreach (UsersTable user in usersResult.Results)
            {
                if (childerns.Contains(user.PartitionKey))
                {
                    childerns.Remove(user.PartitionKey);
                }
            }

            if (childerns.Any())
            {
                try
                {
                    var a = string.Join(ProjectConsts.delimiter, newUserRequest.childrenIds);
                    response.UpdateFailure(string.Format(ErrorMessages.childIdNotFound, string.Join(ProjectConsts.delimiter, newUserRequest.childrenIds)));
                }
                catch( Exception e)
                {
                    System.Console.WriteLine(e);
                }
            }

            return response.requestSucceeded;
        }


        public static List<string> GetSubjectsTableNamesForParent(UsersTable user)
        {
                var subjectsTablesNames = new List<string>();
                var usersTable = CloudTableUtilities.OpenTable(ProjectConsts.UsersTableName);
                var classesTable = CloudTableUtilities.OpenTable(ProjectConsts.classesTableName);
                var childrenIds = user.ChildrenIds.Split(ProjectConsts.delimiter).ToList();

                foreach (var childId in childrenIds)
                {
                    var childEntity = CloudTableUtilities.getTableEntityAsync<UsersTable>(usersTable, childId).Result.First();
                    var childclassEntity = CloudTableUtilities.getTableEntityAsync<ClassesTable>(classesTable, childEntity.TeacherName, childEntity.ClassId).Result.First();
                    subjectsTablesNames.Add(childclassEntity.subjectsTableName);
                }
            

            return subjectsTablesNames;
        }

        internal static string GetUniqueName(string userName)
        {
            var classesTable = CloudTableUtilities.OpenTable(ProjectConsts.UsersTableName);
            var classEntity = CloudTableUtilities.getTableEntityAsync<UsersTable>(classesTable, rowKeyConition: userName).Result;
            var numberOfTeachers = classEntity.Where(user => user.UserType == ProjectConsts.TeacherType).ToList().Count;
            return $"{userName}{numberOfTeachers}";
        }

        public static List<string> GetSubjectsTableNamesForStudent(UsersTable user)
        {
            var classesTable = CloudTableUtilities.OpenTable(ProjectConsts.classesTableName);
            var classEntity = CloudTableUtilities.getTableEntityAsync<ClassesTable>(classesTable, user.TeacherName, user.ClassId).Result.First();
            return new List<string>() { classEntity.subjectsTableName };
        }

        public static List<string> GetSubjectsTableNamesForTeacher(UsersTable user)
        {
            var subjectsTablesNames = new List<string>();

            if(user.ClassId == null)
            {
                return subjectsTablesNames;
            }

            var Grades = user.ClassId.Split(ProjectConsts.delimiter).ToList();
            var classesTable = CloudTableUtilities.OpenTable(ProjectConsts.classesTableName);

            foreach(var grade in Grades)
            {
                var classEntity = CloudTableUtilities.getTableEntityAsync<ClassesTable>(classesTable, user.RowKey, grade).Result.First();
                subjectsTablesNames.Add(classEntity.subjectsTableName);
            }

            return subjectsTablesNames;
        }

    }
}
