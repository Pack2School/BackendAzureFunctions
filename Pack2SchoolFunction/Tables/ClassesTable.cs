using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pack2SchoolFunction.Tables
{
    public class ClassesTable : TableEntity
    {
        public string subjectsTableName { get; set; }
    }
}
