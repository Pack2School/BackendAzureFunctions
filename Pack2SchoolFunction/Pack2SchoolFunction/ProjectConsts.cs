using System;
using System.Collections.Generic;
using System.Text;

namespace Pack2SchoolFunctions
{
    public static class ProjectConsts
    {
        #region Delimiters

        public static readonly string delimiter = ",";

        #endregion

        #region Subject Table metadata consts

        public const string classSubjectsPartitionKey = "0";
        public const string NecessityPartitionKey = "1";
        public const string NecessityRowKey = "Necessity";
        public const string SubjectRowKey = "Subjects";
        public const string SubjectPropertyPrefix = "Subject";
        public const string DeviceId = "deviceId";

        public const string StickerRowKey = "stickers";
        public const string BroughtRowKey = "brought";
        public const string NeededSubject = "true";
        public const string NotNeededSubject = "false";
        public const string InsideTheBag = "true";
        public const string NotInsideTheBag = "false";

        #endregion

        #region Subject Table operation 

        public const string AddSubjectOperation = "ADD";
        public const string RenameSubjectOperation = "Rename";
        public const string DeleteSubjectOperation = "Delete";

        #endregion

        #region User Consts

        public const string TeacherType = "teacher";
        public const string StudentType = "student";
        public const string ParentType = "parent";

        #endregion

        #region Tables Name

        public  static readonly string classesTableName = "ClassesTable";
        public static readonly string UsersTableName = "UsersTable";

        #endregion
    }
}
