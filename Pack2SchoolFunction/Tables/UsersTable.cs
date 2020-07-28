using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pack2SchoolFunction.Tables
{
    public class UsersTable : TableEntity
    {
        public string userType { get; set; }
        public string userEmail { get; set; }
        public string userPassword { get; set; }
        public string subjectsTableName { get; set; }
        public string childId { get; set; }
        public string deviceId { get; set; }
    }
}