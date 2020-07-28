using Microsoft.WindowsAzure.Storage.Table;
using Pack2SchoolFunction.Tables;
using Pack2SchoolFunction.Templates;
using System.Collections.Generic;
using System.Linq;

namespace Pack2SchoolFunctions
{
    public static class UsersTableUtilities
    {
        public static bool ValidateInforamtion(TableQuerySegment<UsersTable> usersResult, UserRequest newUserRequest, Response response)
        {
            foreach (UsersTable user in usersResult.Results)
            {
                if (user.PartitionKey == newUserRequest.userId)
                {
                    response.errorMessage = Response.UserExist;
                    response.requestSucceeded = false;
                    return response.requestSucceeded;
                }
            }

            if (newUserRequest.userType == "parent")
            {
                bool foundChild = false;

                foreach (UsersTable user in usersResult.Results)
                {
                    if (user.PartitionKey == newUserRequest.childId)
                    {
                        foundChild = true;
                    }
                }

                if (!foundChild)
                {
                    response.errorMessage = Response.childIdNotFound;
                    response.requestSucceeded = false;
                    ;
                }
            }

            return response.requestSucceeded;
        }

        public static string GetUserIdFromDeviceId(string deviceId)
        {
            var usersTable = CloudTableOperation.OpenTable("UsersTable");
            var allUsers = CloudTableOperation.getTableEntityAsync<UsersTable>(usersTable).Result;
            var userId = " ";
            
            foreach(var user in allUsers)
            {
                if (user.deviceId == deviceId)
                {
                    userId =  user.PartitionKey;
                }
            }

            return userId;

        }

        public static string GetSubjectsTableNameFromDeviceId(string deviceId)
        {
            var usersTable = CloudTableOperation.OpenTable("UsersTable");
            var allUsers = CloudTableOperation.getTableEntityAsync<UsersTable>(usersTable).Result;
            var subjectsTableName = "";

            foreach (var user in allUsers)
            {
                if (user.deviceId == deviceId)
                {
                    subjectsTableName = user.subjectsTableName;
                }
            }

            return subjectsTableName;

        }

        public static List<string> GetSubjectsTableNameFromUserId(string userId)
        {
            var usersTable = CloudTableOperation.OpenTable("UsersTable");
            var userEntity = CloudTableOperation.getTableEntityAsync<UsersTable>(usersTable,userId).Result.First();

            if (userEntity.userType != "parent" )
            {
                return new List<string>(){userEntity.subjectsTableName};
            }

            var childEntity = CloudTableOperation.getTableEntityAsync<UsersTable>(usersTable,userEntity.childId).Result.First();
            return new List<string>(){ childEntity.subjectsTableName};

        }

    }
}
