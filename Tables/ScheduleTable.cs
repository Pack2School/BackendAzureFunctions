using Microsoft.WindowsAzure.Storage.Table;

namespace Pack2SchoolFunctions
{ 
    /// <summary>
    /// Descirbes the columns in the schedule tables 
    /// where partition key and row kays are day 
    /// </summary>
    public class ScheduleTable  : TableEntity
    {
        public string SubjectA { get; set; }
        public string SubjectB { get; set; }
        public string SubjectC { get; set; }
        public string SubjectD { get; set; }
        public string SubjectE { get; set; }
        public string SubjectF { get; set; }
    }
}
