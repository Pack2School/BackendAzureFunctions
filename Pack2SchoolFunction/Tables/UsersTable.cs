using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pack2SchoolFunction.Tables
{
    /// <summary>
    /// Descirbes the columns in the user tables 
    /// where partition key is the user ID and RowKey is the user full name
    /// </summary>
    public class UsersTable : TableEntity
    {
        public string UserType { get; set; }

        public string UserEmail { get; set; }

        public string UserPassword { get; set; }

        public string TeacherName { get; set; }

        public string Grades { get; set; }

        public string ChildrenIds { get; set; }

    }
}