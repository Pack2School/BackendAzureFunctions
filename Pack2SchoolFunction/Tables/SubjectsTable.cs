using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pack2SchoolFunction.Tables
{
    public class SubjectsTable : TableEntity
    {
        public string SubjectA { get; set; }
        public string SubjectB { get; set; }
        public string SubjectC { get; set; }
        public string SubjectD { get; set; }
        public string SubjectE { get; set; }
        public string SubjectF { get; set; }
    }
}
