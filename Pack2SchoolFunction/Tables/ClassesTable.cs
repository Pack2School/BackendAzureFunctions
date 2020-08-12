using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pack2SchoolFunction.Tables
{
    /// <summary>
    /// Descirbes the columns in the classes tabels 
    /// where partition key is the teacher name and and the RowKey is the class identifier 
    /// </summary>
    public class ClassesTable : TableEntity
    {
        public string subjectsTableName { get; set; }
    }
}
