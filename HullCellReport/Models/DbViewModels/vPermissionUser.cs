namespace HullCellReport.Models.DbViewModels
{
    public class vPermissionUser
    {
        public string empno { get; set; }
        public string empnameeng { get; set; }
        public string departmentno { get; set; }
        public string departmentnameeng { get; set; }
        public string positionnameeng { get; set; }
        public bool hasSystemAccess { get; set; }
        public bool canManagePermission { get; set; }
        public bool reportCheck { get; set; }
    }
}
