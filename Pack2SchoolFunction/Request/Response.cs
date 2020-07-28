using System;
using System.Collections.Generic;
using System.Text;

namespace Pack2SchoolFunction.Templates
{
    public class Response
    {
        public static string wrongPassword = "The identification process failed, an incorrect password was entered";
        public static string userNotExist = "The identification process failed, the given user ID does not exist in the system";
        public static string duplicateEmail = "The registration process failed, the given Email already exists in the system";
        public static string UserExist = "The registration process failed, the user is already registered on the system ";
        public static string childIdNotFound = "The registration process failed, the child's ID not found ";
        public static string EmailIsInvalid = "The registration process failed, the given Email was invalid";
        public static string passwordlIsInvalid = "Invalid password, password must contain at least a number, one upper case letter and  8 characters long";
        public static string subjectTableNotExist = "The registration process failed, the given subjects table doesn't exist";

        public string errorMessage;
        public bool requestSucceeded;
        public object data;
  

        public void UpdateFailure (string errorMessage)
        {
            this.errorMessage = errorMessage;
            this.requestSucceeded = false;
        }

        public void UpdateData(object data)
        {
            this.data = data;
            this.requestSucceeded = true;
        }

    }
}
