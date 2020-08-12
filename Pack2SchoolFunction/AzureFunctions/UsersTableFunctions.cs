using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Table;
using Pack2SchoolFunction.Tables;
using Pack2SchoolFunction.Templates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Runtime.InteropServices.WindowsRuntime;
using Newtonsoft.Json;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using System.Reflection;
using Pack2SchoolFunction;
using System.Net.Http.Formatting;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Microsoft.Azure.Documents.SystemFunctions;
using Pack2SchoolFunctions.AzureObjects;
using Microsoft.Azure.Documents;

namespace Pack2SchoolFunctions
{
    public static class UsersTableFunctions
    {

        /// <summary>
        /// Adds the user to the database
        /// </summary>
        /// <param name="request"></param>
        /// <param name="usersTable"></param>
        /// <returns></returns>
        [FunctionName("SignUp")]
        public static async Task<string> SignUp(
             [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestMessage request,
             [Table("UsersTable")] CloudTable usersTable)
        {
            OperationResult response = new OperationResult();
            string childrenIds = null;

            UserRequest newUserRequest = await Utilities.ExtractContent<UserRequest>(request);

            TableQuery<UsersTable> query = new TableQuery<UsersTable>();

            TableQuerySegment<UsersTable> usersResult = await usersTable.ExecuteQuerySegmentedAsync(query, null);

            if (!UsersTableUtilities.ValidateUserNotExist(usersResult, newUserRequest, response))
            {
                return JsonConvert.SerializeObject(response);
            }

            if (newUserRequest.userType == ProjectConsts.ParentType)
            {
                childrenIds = string.Join(ProjectConsts.delimiter, newUserRequest.childrenId);
                if (!UsersTableUtilities.ValidateChildrenIdExist(usersResult, newUserRequest, response))
                {
                    return JsonConvert.SerializeObject(response);
                }
            }

            var newUser = new UsersTable()
            {
                UserType = newUserRequest.userType,
                UserEmail = newUserRequest.userEmail,
                UserPassword = newUserRequest.userPassword,
                TeacherName = newUserRequest.teacherName,
                Grades = newUserRequest.grade,
                ChildrenIds = childrenIds
            };

            await CloudTableUtilities.AddTableEntity<UsersTable>(usersTable, newUser, newUserRequest.userId, newUserRequest.userName);
           
            if (newUserRequest.userType == ProjectConsts.ParentType)
            {
                response.Data = UsersTableUtilities.GetSubjectsTableNamesForParent(newUser);
            }

            return JsonConvert.SerializeObject(response);
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="usersTable"></param>
        /// <returns></returns>
        [FunctionName("SignIn")]
        public static async Task<string> SignIn(

           [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestMessage request,
           [Table("UsersTable")] CloudTable usersTable)
        {
            OperationResult response = new OperationResult();
            UserRequest userRequest = await Utilities.ExtractContent<UserRequest>(request);

            TableQuery<UsersTable> query = new TableQuery<UsersTable>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, userRequest.userId));

            TableQuerySegment<UsersTable> subjectNamequeryResult = await usersTable.ExecuteQuerySegmentedAsync(query, null);

            if (!subjectNamequeryResult.Results.Any())
            {
                response.UpdateFailure(ErrorMessages.userNotExist);
            }
            else
            {
                var user = subjectNamequeryResult.Results.First();

                if (user.UserPassword == userRequest.userPassword)
                {
                    List<string> subjectsTablesNames;

                    switch (user.UserType) 
                    {
                        case ProjectConsts.ParentType:
                            subjectsTablesNames = UsersTableUtilities.GetSubjectsTableNamesForParent(user);
                            break;
                        case ProjectConsts.TeacherType:
                            subjectsTablesNames = UsersTableUtilities.GetSubjectsTableNamesForTeacher(user);
                            break;
                        default:
                            subjectsTablesNames = UsersTableUtilities.GetSubjectsTableNamesForStudent(user);
                            break;
                    }

                    response.UpdateData(new { user.UserType, subjectsTablesNames });
                }
                else
                {
                    response.UpdateFailure(ErrorMessages.wrongPassword);
                }
            }

            return JsonConvert.SerializeObject(response);
        }
    }
}
