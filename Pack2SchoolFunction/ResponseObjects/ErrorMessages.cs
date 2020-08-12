using System;
using System.Collections.Generic;
using System.Text;

namespace Pack2SchoolFunctions
{
    /// <summary>
    /// sets all error messages 
    /// </summary>
    public static class ErrorMessages
    {
        #region Login issues

        public static string wrongPassword = "The identification process failed, an incorrect password was entered";
        public static string userNotExist = "The identification process failed, the given user ID does not exist in the system";

        #endregion

        #region registration issues

        public static string UserExist = "The registration process failed, the user is already registered on the system ";
        public static string childIdNotFound = "The registration process failed, the following childrens id not found {0}";
        public static string EmailIsInvalid = "The registration process failed, the given Email was invalid";
        public static string passwordlIsInvalid = "Invalid password, password must contain at least a number, one upper case letter and  8 characters long";
        #endregion

        #region classes issues

        public static string subjectTableNotExist = "The registration process failed, the given subjects table doesn't exist";
        public static string classAlreadyExist = "The class creation process fail, there is class in the database with same details";

        #endregion

        #region Subjects table issues

        public static string noSubjectsAvailable = "The process of adding a new subject failed, all subjects were utilized";
        public static string subjetNotFound = "the given subject not found";
        #endregion 
    }
}
