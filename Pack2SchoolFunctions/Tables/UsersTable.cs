using Microsoft.WindowsAzure.Storage.Table;

namespace Pack2SchoolFunctions
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

        public string ClassId { get; set; }

        public string ChildrenIds { get; set; }

    }
}