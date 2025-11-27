using HullCellReport.Models.DbViewModels;
using HullCellReport.Repositories;
using JWTRegen.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HullCellReport.Controllers
{
    public class AuthController : BaseController
    {
        private readonly EmployeeRepository _emp;
        private readonly IJwtTokenService _jwtTokenService;

        public AuthController(
            EmployeeRepository emp,
            IJwtTokenService jwtTokenService)
        {
            _emp = emp;
            _jwtTokenService = jwtTokenService;
        }
        public IActionResult vLogin()
        {
            // Prevent caching of login page
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";
            
            return View();
        }

        public class LoginVM
        {
            public string txt_empno { get; set; }
            public string txt_password { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginVM form)
        {
            try
            {
                if (await ValLogin(form))
                {
                    return GenerateErrorResponse();
                }
               

                var token = _jwtTokenService.GenerateToken(form.txt_empno, "user");

                Response.Cookies.Append("hullcellreport_jwt", token, new CookieOptions
                {
                    HttpOnly = true,
                    //Secure = true, disable when use http
                    SameSite = SameSiteMode.Strict,
                    Path = "/", // Set cookie available across the entire site
                    Expires = DateTime.UtcNow.AddHours(24)
                });
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, text = ex.Message });
            }
        }

        private async Task<bool> ValLogin(LoginVM form)
        {
            bool result = false;

            // ตรวจสอบข้อมูลที่จำเป็น
            if (string.IsNullOrWhiteSpace(form.txt_empno))
            {
                ModelState.AddModelError("txt_empno", "กรุณากรอกรหัสพนักงาน");
                result = true;
            }

            if (string.IsNullOrWhiteSpace(form.txt_password))
            {
                ModelState.AddModelError("txt_password", "กรุณากรอกรหัสผ่าน");
                result = true;
            }

            // ถ้าข้อมูลไม่ครบ ให้หยุดตรวจสอบ
            if (result)
            {
                return result;
            }

            await _emp.Login(form.txt_empno, form.txt_password);
            vw_emp obj_emp = await _emp.GetByEmpno(form.txt_empno, true);
            
            if (obj_emp.empstatusno == "R")
            {
                ModelState.AddModelError("txt_empno", "พนักงานลาออกแล้ว");
                return true;
            }
            
            return result;
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            Response.Cookies.Append("hullcellreport_jwt", string.Empty, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Strict,
                Path = "/", // Set cookie available across the entire site
                Expires = DateTime.UtcNow.AddDays(-1)
            });
            
            // Prevent caching
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";
            
            return RedirectToAction("vLogin", "Auth");
        }
    }
}
