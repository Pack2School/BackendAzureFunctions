using System;
using System.Collections.Generic;
using System.Text;

namespace Pack2SchoolFunction.Templates
{
    public class SchoolClass
    {
        public string teacherName;
        public string grade;

        public override string ToString()
        {
            return $"{teacherName}{grade}";
        }
    }
}
