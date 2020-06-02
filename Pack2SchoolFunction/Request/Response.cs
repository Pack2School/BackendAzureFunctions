using System;
using System.Collections.Generic;
using System.Text;

namespace Pack2SchoolFunction.Templates
{
    public class Response<T>
    {
        public string errorMessage;
        public bool requestSucceeded;
        public T data;
        

        public static string wrongPassword = "The identification process failed, an incorrect password was entered";
        public static string userNotExist = "The identification process failed, the given Email does not exist in the system";
        public static string duplicateEmail = "The registration process failed, the given Email already exists in the system";
        public static string duplicateId = "The registration process failed, the given ID already exisits in the system ";
        public static string childIdNotFound = "The registration process failed, the child's ID not found ";
        public static string EmailIsInvalid = "The registration process failed, the given Email was invalid";
        public static string passwordlIsInvalid = "Invalid password, password must contain at least a number, one upper case letter and  8 characters long";


    }
}
