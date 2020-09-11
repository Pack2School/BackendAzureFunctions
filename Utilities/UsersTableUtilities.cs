using Microsoft.WindowsAzure.Storage.Table;
using Pack2SchoolFunction.Templates;
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
                    break;
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
                    response.UpdateFailure(string.Format(ErrorMessages.childIdNotFound, string.Join(ProjectConsts.delimiter, childerns))); 
            }

            return response.requestSucceeded;
        }

        internal static string GetUniqueName(string userName)
        {
            var usersTable = CloudTableUtilities.OpenTable(ProjectConsts.UsersTableName);
            var usersEntities = CloudTableUtilities.getTableEntityAsync<UsersTable>(usersTable).Result;
            int numberOfTeacher = usersEntities.Where(user => user.RowKey.Contains(userName) && user.UserType == ProjectConsts.TeacherType).Count();
            return $"{userName}{numberOfTeacher}";
        }

        public static List<string> GetSubjectsTableNamesForStudent(UsersTable userEntity)
        {
            var classesTable = CloudTableUtilities.OpenTable(ProjectConsts.classesTableName);
            var classEntity = CloudTableUtilities.getTableEntityAsync<ClassesTable>(classesTable, userEntity.TeacherName, userEntity.ClassId).Result.First();
            return new List<string>() { classEntity.subjectsTableName };
        }

        public static List<string> GetSubjectsTableNamesForTeacher(UsersTable userEntity)
        {
            var subjectsTablesNames = new List<string>();

            if(userEntity.ClassId == null)
            {
                return subjectsTablesNames;
            }

            var classesIds = userEntity.ClassId.Split(ProjectConsts.delimiter).ToList();
            var classesTable = CloudTableUtilities.OpenTable(ProjectConsts.classesTableName);

            foreach(var classId in classesIds)
            {
                var classEntity = CloudTableUtilities.getTableEntityAsync<ClassesTable>(classesTable, userEntity.RowKey, classId).Result.First();
                subjectsTablesNames.Add(classEntity.subjectsTableName);
            }

            return subjectsTablesNames;
        }

        public static List<UsersTable> GetParentsEntitiesFromChildId(string childId)
        {
            var parentsIds = new List<UsersTable>();
            var usersTable = CloudTableUtilities.OpenTable(ProjectConsts.UsersTableName);
            var usersEntity = CloudTableUtilities.getTableEntityAsync<UsersTable>(usersTable).Result;

            foreach( var userEntity in usersEntity)
            {
                if (userEntity.UserType != ProjectConsts.ParentType)
                {
                    continue;
                }

                var childrenIds = userEntity.ChildrenIds.Split(ProjectConsts.delimiter).ToList();
                if (childrenIds.Contains(childId))
                {
                    parentsIds.Add(userEntity);
                }
            }

            return parentsIds;
        }

    }
}
