using CheckingSupplierEmail.Models.DbViewModels;
using CheckingSupplierEmail.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CheckingSupplierEmail.Repositories
{
    public class EmployeeRepository
    {
        private readonly DapperService _dapper;

        public EmployeeRepository(
            DapperService dapper)
        {
            _dapper = dapper;
        }

        public async Task<vw_emp> GetByEmpno(string txt_empno)
        {
            string sql = $@"SELECT [empno], [empnameeng], [empstatusno], [departmentno] FROM [vw_emp] WHERE [empno] = @txt_empno";
            return await _dapper.QueryFirst<vw_emp>("1", sql, new { txt_empno });
        }

        public async Task<bool> Login(string empno, string password)
        {
            string sql = $"SELECT [username] FROM [vw_username_subcon] " +
                $"WHERE [username] = @u AND " +
                $"[userpasshash] = HASHBYTES('SHA', '{password}')";
            var data = await _dapper.QueryFirst("1", sql, new { u = empno });
            if (data == null)
                throw new Exception("ชื่อผู้ใช้หรือรหัสผ่านไม่ถูกต้อง");
            return true;
        }
    }
}
