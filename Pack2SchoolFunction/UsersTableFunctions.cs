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

        // User: all
        // Purpose: To create new account
        [FunctionName("SignUp")]
        public static async Task<string> SignUp(
             [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestMessage request,
             [Table("UsersTable")] CloudTable usersTable, ILogger log)
        {
            Response response = new Response() { requestSucceeded = true };

            
            UserRequest newUserRequest = await Utilities.ExtractContent<UserRequest>(request);

            TableQuery<UsersTable> query = new TableQuery<UsersTable>();

            TableQuerySegment<UsersTable> usersResult = await usersTable.ExecuteQuerySegmentedAsync(query, null);

            if (!UsersTableUtilities.ValidateInforamtion(usersResult, newUserRequest, response))
            {
                return JsonConvert.SerializeObject(response);
            }

            if (newUserRequest.userType == "student")
            {
                await SubjectsTableUtilities.AddStuentToClassTableAsync(newUserRequest, response);
                response.data = IotDeviceFunctions.AddDeviceAsync(newUserRequest.userId).Result;
            }

            if (response.requestSucceeded)
            {
                
                UsersTable newUser = new UsersTable() {PartitionKey  = newUserRequest.userId,  RowKey = newUserRequest.userName, userEmail = newUserRequest.userEmail, userPassword = newUserRequest.userPassword, userType = newUserRequest.userType, childId = newUserRequest.childId, subjectsTableName = newUserRequest.subjectsTableName };
                await CloudTableOperation.AddTableEntity<UsersTable>(usersTable, newUser, newUserRequest.userId, newUserRequest.userName);
            }


            if (newUserRequest.userType == "parent")
            {
                response.data = UsersTableUtilities.GetSubjectsTableNameFromUserId(newUserRequest.userId);
            }

            return JsonConvert.SerializeObject(response);
        }

        // User: all
        // Purpose: to sign in to app
        [FunctionName("SignIn")]
        public static async Task<string> SignIn(

           [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestMessage request,
           [Table("UsersTable")] CloudTable usersTable, ILogger log)
        {
            Response response = new Response();
            UserRequest userRequest = await Utilities.ExtractContent<UserRequest>(request);

            TableQuery<UsersTable> query = new TableQuery<UsersTable>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, userRequest.userId));

            TableQuerySegment<UsersTable> subjectNamequeryResult = await usersTable.ExecuteQuerySegmentedAsync(query, null);

            foreach (var user in subjectNamequeryResult.Results)
            {
                if (user.userPassword == userRequest.userPassword)
                {
                    var subjectsTablesNames = UsersTableUtilities.GetSubjectsTableNameFromUserId(userRequest.userId);
                    var data = new { user.userType, subjectsTablesNames};
                    response.UpdateData(data);
                }
                else
                {
                    response.UpdateFailure(Response.wrongPassword);
                }
            }

            if (!subjectNamequeryResult.Results.Any())
            {
                response.UpdateFailure(Response.userNotExist);
            }

            return JsonConvert.SerializeObject(response);
        }
    }
}
