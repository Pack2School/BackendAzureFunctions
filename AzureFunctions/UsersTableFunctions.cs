using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using Pack2SchoolFunction;
using Pack2SchoolFunction.Templates;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Pack2SchoolFunctions
{
    public static class UsersTableFunctions
    {

        [FunctionName("SignUp")]
        public static async Task<string> SignUp(
             [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestMessage request,
             [Table("UsersTable")] CloudTable usersTable)
        {
            OperationResult operationResult = new OperationResult();
            string childrenIds = null;

            UserRequest newUserRequest = await Utilities.ExtractContent<UserRequest>(request);

            TableQuery<UsersTable> query = new TableQuery<UsersTable>();

            TableQuerySegment<UsersTable> usersQueryResult = await usersTable.ExecuteQuerySegmentedAsync(query, null);

            if (!UsersTableUtilities.ValidateUserNotExist(usersQueryResult, newUserRequest, operationResult))
            {
                return JsonConvert.SerializeObject(operationResult);
            }

            if (newUserRequest.userType == ProjectConsts.ParentType)
            {
                if (newUserRequest.childrenIds == null || newUserRequest.childrenIds.Count == 0)
                {
                    operationResult.UpdateFailure(ErrorMessages.NoChildIdProvided);
                    return JsonConvert.SerializeObject(operationResult);
                }

                childrenIds = string.Join(ProjectConsts.delimiter, newUserRequest.childrenIds);

                if (!UsersTableUtilities.ValidateChildrenIdExist(usersQueryResult, newUserRequest, operationResult))
                {
                    return JsonConvert.SerializeObject(operationResult);
                }
            }


            if (newUserRequest.userType == ProjectConsts.TeacherType)
            {
                newUserRequest.userName = UsersTableUtilities.GetUniqueName(newUserRequest.userName);
            }

            if (newUserRequest.userType == ProjectConsts.StudentType)
            {
                var tableExist = CloudTableUtilities.TableExist(newUserRequest.teacherUser + newUserRequest.classId);

                if (!tableExist)
                {
                    operationResult.UpdateFailure(string.Format(ErrorMessages.subjectTableNotExist));
                    return JsonConvert.SerializeObject(operationResult);
                }

            }
        
            var newUserEntity = new UsersTable()
            {
                UserType = newUserRequest.userType,
                UserEmail = newUserRequest.userEmail,
                UserPassword = newUserRequest.userPassword,
                TeacherName = newUserRequest.teacherUser,
                ClassId = newUserRequest.classId,
                ChildrenIds = childrenIds
            };

            await CloudTableUtilities.AddTableEntity(usersTable, newUserEntity);
           
            if (newUserRequest.userType == ProjectConsts.TeacherType)
            {
                operationResult.UpdateData(newUserEntity.RowKey);
            }


            if (newUserRequest.userType == ProjectConsts.StudentType)
            {
                var deviceConnectionString = await IotDeviceFunctions.AddDeviceAsync(newUserRequest.userId);
                var subjectsTablesNames = UsersTableUtilities.GetSubjectsTableNamesForStudent(newUserEntity);
                await SubjectsTableUtilities.AddStuentToClassTableAsync(subjectsTablesNames.First(), newUserRequest, operationResult);
                operationResult.UpdateData(new { deviceConnectionString, subjectsTablesNames });
            }

            return JsonConvert.SerializeObject(operationResult);
        }
        
        [FunctionName("SignIn")]
        public static async Task<string> SignIn(

           [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestMessage request,
           [Table("UsersTable")] CloudTable usersTable)
        {
            OperationResult operationResult = new OperationResult();
            UserRequest userRequest = await Utilities.ExtractContent<UserRequest>(request);

            TableQuery<UsersTable> query = new TableQuery<UsersTable>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, userRequest.userId));

            var usersQueryResult = await usersTable.ExecuteQuerySegmentedAsync(query, null);

            if (!usersQueryResult.Results.Any())
            {
                operationResult.UpdateFailure(ErrorMessages.userNotExist);
            }
            else
            {
                var user = usersQueryResult.Results.First();

                if (user.UserPassword == userRequest.userPassword)
                {
                    List<string> Info;

                    switch (user.UserType) 
                    {
                        case ProjectConsts.ParentType:
                            Info = user.ChildrenIds.Split(ProjectConsts.delimiter).ToList();
                            break;
                        case ProjectConsts.TeacherType:
                            Info = UsersTableUtilities.GetSubjectsTableNamesForTeacher(user);
                            break;
                        default:
                            Info = UsersTableUtilities.GetSubjectsTableNamesForStudent(user);
                            break;
                    }

                    operationResult.UpdateData(new { userName = user.RowKey, userType = user.UserType, Info });        
                }
                else
                {
                    operationResult.UpdateFailure(ErrorMessages.wrongPassword);
                }
            }

            return JsonConvert.SerializeObject(operationResult);
        }

        [FunctionName("GetChildSubjectsTableName")]
        public static async Task<string> GetChildSubjectsTableName(

           [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestMessage request,
           [Table("UsersTable")] CloudTable usersTable)
        {
            OperationResult operationResult = new OperationResult();

            UserRequest userRequest = await Utilities.ExtractContent<UserRequest>(request);
            var userChildEntity = CloudTableUtilities.getTableEntityAsync<UsersTable>(usersTable, userRequest.childrenIds.First()).Result.First();
            var subjectsTableName = UsersTableUtilities.GetSubjectsTableNamesForStudent(userChildEntity);

            operationResult.UpdateData(subjectsTableName);
            return JsonConvert.SerializeObject(operationResult);
        }
    }
}
