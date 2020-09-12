using System;
using System.Collections.Generic;

namespace Pack2SchoolFunctions
{
    public class DataBaseAndScanUpdates
    {
        public string studentId { set; get; }

        public string errorMessage { set; get; }

        public List<String> neededSubjects { set; get; }

        public List<String> missingSubjects { set; get; }

        public List<String> allSubjects { set; get; }

        public List<String>  extraSubjects { set; get; }

        public DataBaseAndScanUpdates(string studentId, List<String> neededSubjects = null, List<String> missingSubjects = null, List<String> allSubjects = null, List<String> extraSubjects = null)
        {
            this.studentId = studentId;
            this.neededSubjects = neededSubjects;
            this.missingSubjects = missingSubjects;
            this.allSubjects = allSubjects;
            this.extraSubjects = extraSubjects;
        }

        public DataBaseAndScanUpdates(string errorMessage)
        {
            this.errorMessage = errorMessage;
        }


    }
}
