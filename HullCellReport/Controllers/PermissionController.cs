using HullCellReport.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using JWTRegen.Interfaces;
using System.Linq;

namespace HullCellReport.Controllers
{
    public class PermissionController : BaseController
    {
        private readonly ILogger<PermissionController> _logger;
        private readonly IClaimsHelper _claimsHelper;
        private readonly EmployeeRepository _employeeRepo;

        public PermissionController(
            ILogger<PermissionController> logger,
            EmployeeRepository employeeRepo,
            JWTRegen.Interfaces.IClaimsHelper claimsHelper)
        {
            _logger = logger;
            _employeeRepo = employeeRepo;
            _claimsHelper = claimsHelper;
        }

        public IActionResult vPermission()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> CheckPermissionMenuAccess()
        {
            try
            {
                var username = _claimsHelper.GetUserId(User) ?? "ADMIN";
                if (string.IsNullOrEmpty(username))
                {
                    return Json(new { success = true, hasAccess = false });
                }

                var hasAccess = await _employeeRepo.CheckPermissionMenuAccess(username);
                return Json(new { success = true, hasAccess });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking permission menu access");
                return Json(new { success = false, hasAccess = false });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers(int page = 1, int pageSize = 10, string search = "", string sortOrder = "asc")
        {
            try
            {
                var result = await _employeeRepo.GetUsersWithPermissions(page, pageSize, search, sortOrder);
                
                return Json(new { success = true, data = result.users, total = result.total });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateSystemAccess([FromBody] UpdatePermissionRequest request)
        {
            try
            {
                await _employeeRepo.UpdateSystemAccess(request.username, request.hasAccess);
                return Json(new { success = true, message = "อัพเดทสิทธิ์การเข้าใช้งานระบบสำเร็จ" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating system access");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateManagePermission([FromBody] UpdatePermissionRequest request)
        {
            try
            {
                await _employeeRepo.UpdateManagePermissionAccess(request.username, request.hasAccess);
                return Json(new { success = true, message = "อัพเดทสิทธิ์การจัดการสิทธิ์สำเร็จ" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating manage permission");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateReportCheckAccess([FromBody] UpdatePermissionRequest request)
        {
            try
            {
                await _employeeRepo.UpdateReportCheckAccess(request.username, request.hasAccess);
                return Json(new { success = true, message = "อัพเดทสิทธิ์ Report Check สำเร็จ" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating report check access");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> CheckEmployee(string empno)
        {
            try
            {
                var employee = await _employeeRepo.GetByEmpno(empno, false);
                
                if (employee == null)
                {
                    return Json(new { success = false, message = "ไม่พบพนักงานในระบบ" });
                }

                // เช็คว่ามีในตาราง usermenu แล้วหรือยัง
                var alreadyExists = await _employeeRepo.CheckEmployeeInPermissionList(empno);
                
                return Json(new { 
                    success = true, 
                    data = employee, 
                    alreadyExists = alreadyExists 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking employee");
                return Json(new { success = false, message = "เกิดข้อผิดพลาดในการตรวจสอบข้อมูล" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddEmployee([FromBody] AddEmployeeRequest request)
        {
            try
            {
                var employee = await _employeeRepo.GetByEmpno(request.empno, false);
                
                if (employee == null)
                {
                    return Json(new { success = false, message = "ไม่พบพนักงานในระบบ" });
                }

                var alreadyExists = await _employeeRepo.CheckEmployeeInPermissionList(request.empno);
                
                if (alreadyExists)
                {
                    return Json(new { success = false, message = "พนักงานคนนี้มีอยู่ในระบบแล้ว" });
                }

                // เพิ่มสิทธิ์เข้าใช้งานระบบ (mnuid = 1513) ให้กับพนักงานใหม่
                await _employeeRepo.UpdateSystemAccess(request.empno, true);
                
                return Json(new { success = true, message = "เพิ่มพนักงานเข้าระบบสำเร็จ" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding employee");
                return Json(new { success = false, message = ex.Message });
            }
        }
    }

    public class UpdatePermissionRequest
    {
        public string username { get; set; }
        public bool hasAccess { get; set; }
    }

    public class AddEmployeeRequest
    {
        public string empno { get; set; }
    }
    
}
