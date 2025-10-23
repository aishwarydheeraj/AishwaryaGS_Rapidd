using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HtmlTable.Models
{
    public class EmployeeModel
    {
        public string Id { get; set; }
        public string EmployeeName { get; set; }
        public string StarTimeUtc { get; set; }
        public string EndTimeUtc { get; set; }
        public string EntryNotes { get; set; }
        public string DeletedOn { get; set; }
    }
}
