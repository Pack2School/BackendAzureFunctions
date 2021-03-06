﻿using Microsoft.WindowsAzure.Storage.Table;
using System;

namespace Pack2SchoolFunctions
{ 
    /// <summary>
    /// Descirbes the columns in the classes tabels 
    /// where partition key is the teacher name and and the RowKey is the class identifier 
    /// </summary>
    public class ClassesTable : TableEntity
    {
        public string subjectsTableName { get; set; }

        public string ScheduleTableName { get; set; }

        public DateTime LastTeacherUpdate { get; set; }
    }
}
