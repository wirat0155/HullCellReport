using HullCellReport.Models.DbViewModels;
using HullCellReport.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HullCellReport.Repositories
{
    public class EmployeeRepository
    {
        private readonly DapperService _dapper;

        public EmployeeRepository(
            DapperService dapper)
        {
            _dapper = dapper;
        }

        public async Task<vw_emp> GetByEmpno(string txt_empno, bool requirePermission1513 = true)
        {
            string sql;
            
            if (requirePermission1513)
            {
                // เอาเฉพาะพนักงานที่มีสิทธิ์ 1513
                sql = $@"SELECT [empno], [empnameeng], [empnameengshort1], [empstatusno], [departmentno], [departmentnameeng], [positionnameeng] 
                        FROM [vw_emp] 
                        WHERE [empno] = @txt_empno
                        AND EXISTS (
                            SELECT 1 FROM [usermenu] 
                            WHERE [username] = @txt_empno 
                            AND [mnuid] = 1513 
                            AND [id_enabled] = 1
                        )";
            }
            else
            {
                // พนักงานคนใดก็ได้
                sql = $@"SELECT [empno], [empnameeng], [empnameengshort1], [empstatusno], [departmentno], [departmentnameeng], [positionnameeng] 
                        FROM [vw_emp] 
                        WHERE [empno] = @txt_empno";
            }
            
            return await _dapper.QueryFirst<vw_emp>("UICT", sql, new { txt_empno });
        }

        public async Task<bool> CheckEmployeeInPermissionList(string empno)
        {
            string sql = @"SELECT COUNT(*) FROM [usermenu] 
                          WHERE [username] = @empno 
                          AND ([mnuid] = 1513 OR [mnuid] = 1514 OR [mnuid] = 1515)";
            var count = await _dapper.ExecuteScalar<int>("UICT", sql, new { empno });
            return count > 0;
        }

        public async Task<bool> CheckPermissionMenuAccess(string username)
        {
            string sql = @"SELECT * FROM [usermenu] 
                          WHERE [username] = @username 
                          AND [mnuid] = 1514 
                          AND [id_enabled] = 1 
                          AND [id_visible] = 1";
            var result = await _dapper.QueryFirst("UICT", sql, new { username });
            return result != null;
        }

        public async Task<(IEnumerable<vPermissionUser> users, int total)> GetUsersWithPermissions(
            int page = 1, 
            int pageSize = 10, 
            string search = "", 
            string sortOrder = "asc")
        {
            // กัน single-quote กลายเป็นปัญหา SQL
            search = (search ?? "").Replace("'", "''");
            
            string orderByClause = sortOrder.ToLower() == "desc" ? "DESC" : "ASC";

            // นับจำนวนทั้งหมด
            string countSql = $@"
                SELECT COUNT(DISTINCT e.empno)
                FROM vw_emp e
                LEFT JOIN usermenu um1 
                    ON e.empno = um1.username 
                    AND um1.mnuid = 1513
                LEFT JOIN usermenu um2 
                    ON e.empno = um2.username
                    AND um2.mnuid = 1514
                LEFT JOIN usermenu um3 
                    ON e.empno = um3.username
                    AND um3.mnuid = 1515
                WHERE 
                    e.empstatusno = 'N'
                    AND (um1.username IS NOT NULL OR um2.username IS NOT NULL OR um3.username IS NOT NULL)
                    AND ('{search}' = '' 
                        OR e.empno LIKE '%{search}%'
                        OR e.empnameeng LIKE '%{search}%'
                        OR e.departmentnameeng LIKE '%{search}%'
                        OR e.positionnameeng LIKE '%{search}%'
                    )";

            var total = await _dapper.ExecuteScalar<int>("UICT", countSql);

            // ดึงข้อมูลแบบ pagination
            string sql = $@"
                SELECT DISTINCT
                    e.empno,
                    e.empnameeng,
                    e.departmentno,
                    e.departmentnameeng,
                    e.empstatusno,
                    e.positionnameeng,
                    CASE 
                        WHEN um1.username IS NOT NULL AND um1.id_enabled = 1 
                        THEN 1 ELSE 0 
                    END AS hasSystemAccess,
                    CASE 
                        WHEN um2.username IS NOT NULL 
                             AND um2.id_enabled = 1 
                             AND um2.id_visible = 1 
                        THEN 1 ELSE 0 
                    END AS canManagePermission,
                    CASE 
                        WHEN um3.username IS NOT NULL 
                             AND um3.id_enabled = 1 
                             AND um3.id_visible = 1 
                        THEN 1 ELSE 0 
                    END AS reportCheck
                FROM vw_emp e
                LEFT JOIN usermenu um1 
                    ON e.empno = um1.username 
                    AND um1.mnuid = 1513
                LEFT JOIN usermenu um2 
                    ON e.empno = um2.username
                    AND um2.mnuid = 1514
                LEFT JOIN usermenu um3 
                    ON e.empno = um3.username
                    AND um3.mnuid = 1515
                WHERE 
                    e.empstatusno = 'N'
                    AND (um1.username IS NOT NULL OR um2.username IS NOT NULL OR um3.username IS NOT NULL)
                    AND ('{search}' = '' 
                        OR e.empno LIKE '%{search}%'
                        OR e.empnameeng LIKE '%{search}%'
                        OR e.departmentnameeng LIKE '%{search}%'
                        OR e.positionnameeng LIKE '%{search}%'
                    )
                ORDER BY e.empno {orderByClause}
                OFFSET {(page - 1) * pageSize} ROWS
                FETCH NEXT {pageSize} ROWS ONLY";

            var users = await _dapper.Query<vPermissionUser>("UICT", sql);
            
            return (users, total);
        }


        public async Task<bool> UpdateSystemAccess(string username, bool hasAccess)
        {
            if (hasAccess)
            {
                // เพิ่มสิทธิ์เข้าใช้งานระบบ (mnuid = 1513)
                string checkSql = @"SELECT COUNT(*) FROM usermenu 
                                   WHERE username = @username AND mnuid = 1513";
                var exists = await _dapper.ExecuteScalar<int>("UICT", checkSql, new { username });
                
                if (exists == 0)
                {
                    string insertSql = @"INSERT INTO usermenu (username, mnuid, id_enabled, id_visible) 
                                        VALUES (@username, 1513, 1, 1)";
                    await _dapper.Execute("UICT", insertSql, new { username });
                }
                else
                {
                    string updateSql = @"UPDATE usermenu 
                                        SET id_enabled = 1 
                                        WHERE username = @username AND mnuid = 1513";
                    await _dapper.Execute("UICT", updateSql, new { username });
                }
            }
            else
            {
                // ปิดสิทธิ์เข้าใช้งานระบบ
                string updateSql = @"UPDATE usermenu 
                                    SET id_enabled = 0 
                                    WHERE username = @username AND mnuid = 1513";
                await _dapper.Execute("UICT", updateSql, new { username });
            }
            
            return true;
        }

        public async Task<bool> UpdateManagePermissionAccess(string username, bool hasAccess)
        {
            if (hasAccess)
            {
                // เพิ่มสิทธิ์จัดการสิทธิ์ (mnuid = 1514)
                string checkSql = @"SELECT COUNT(*) FROM usermenu 
                                   WHERE username = @username AND mnuid = 1514";
                var exists = await _dapper.ExecuteScalar<int>("UICT", checkSql, new { username });
                
                if (exists == 0)
                {
                    string insertSql = @"INSERT INTO usermenu (username, mnuid, id_enabled, id_visible) 
                                        VALUES (@username, 1514, 1, 1)";
                    await _dapper.Execute("UICT", insertSql, new { username });
                }
                else
                {
                    string updateSql = @"UPDATE usermenu 
                                        SET id_enabled = 1, id_visible = 1 
                                        WHERE username = @username AND mnuid = 1514";
                    await _dapper.Execute("UICT", updateSql, new { username });
                }
            }
            else
            {
                // ปิดสิทธิ์จัดการสิทธิ์
                string updateSql = @"UPDATE usermenu 
                                    SET id_enabled = 0, id_visible = 0 
                                    WHERE username = @username AND mnuid = 1514";
                await _dapper.Execute("UICT", updateSql, new { username });
            }
            
            return true;
        }

        public async Task<bool> UpdateReportCheckAccess(string username, bool hasAccess)
        {
            if (hasAccess)
            {
                // เพิ่มสิทธิ์ Report Check (mnuid = 1515)
                string checkSql = @"SELECT COUNT(*) FROM usermenu 
                                   WHERE username = @username AND mnuid = 1515";
                var exists = await _dapper.ExecuteScalar<int>("UICT", checkSql, new { username });
                
                if (exists == 0)
                {
                    string insertSql = @"INSERT INTO usermenu (username, mnuid, id_enabled, id_visible) 
                                        VALUES (@username, 1515, 1, 1)";
                    await _dapper.Execute("UICT", insertSql, new { username });
                }
                else
                {
                    string updateSql = @"UPDATE usermenu 
                                        SET id_enabled = 1, id_visible = 1 
                                        WHERE username = @username AND mnuid = 1515";
                    await _dapper.Execute("UICT", updateSql, new { username });
                }
            }
            else
            {
                // ปิดสิทธิ์ Report Check
                string updateSql = @"UPDATE usermenu 
                                    SET id_enabled = 0, id_visible = 0 
                                    WHERE username = @username AND mnuid = 1515";
                await _dapper.Execute("UICT", updateSql, new { username });
            }
            
            return true;
        }

        public async Task<Dictionary<string, string>> GetEmployeeNamesByEmpnos(IEnumerable<string> empnos)
        {
            if (empnos == null || !empnos.Any())
                return new Dictionary<string, string>();

            var uniqueEmpnos = empnos.Distinct().ToList();
            var empnoList = string.Join(",", uniqueEmpnos.Select(e => $"'{e.Replace("'", "''")}'"));
            
            string sql = $@"SELECT [empno], [empnameeng] 
                           FROM [vw_emp] 
                           WHERE [empno] IN ({empnoList})";
            
            var employees = await _dapper.Query<vw_emp>("UICT", sql);
            
            return employees.ToDictionary(e => e.empno, e => e.empnameeng ?? "");
        }

        public async Task<bool> Login(string empno, string password)
        {
            string sql = $"SELECT [username] FROM [vw_username_subcon] " +
                $"WHERE [username] = @u AND " +
                $"[userpasshash] = HASHBYTES('SHA', '{password}')";
            var data = await _dapper.QueryFirst("UICT", sql, new { u = empno });
            if (data == null)
                throw new Exception("ชื่อผู้ใช้หรือรหัสผ่านไม่ถูกต้อง");
            
            // Check if user has permission to access this system (mnuid = 1513)`
            string permissionSql = @"SELECT * FROM [usermenu] 
                                    WHERE [username] = @username 
                                    AND [mnuid] = 1513 
                                    AND [id_enabled] = 1";
            var permission = await _dapper.QueryFirst("UICT", permissionSql, new { username = empno });
            
            if (permission == null)
                throw new Exception("คุณไม่มีสิทธิ์เข้าใช้งานระบบนี้");
            
            return true;
        }

        public async Task<bool> CheckPermissionReportCheck(string empno)
        {
            // mnuid 1515 = Report Check Permission
            string sql = @"SELECT COUNT(*) FROM [usermenu] 
                          WHERE [username] = @empno 
                          AND [mnuid] = 1515 
                          AND [id_enabled] = 1";
            var count = await _dapper.ExecuteScalar<int>("UICT", sql, new { empno });
            return count > 0;
        }
    }
}
