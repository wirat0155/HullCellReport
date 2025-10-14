using CheckingSupplierEmail.Models.DbViewModels;
using CheckingSupplierEmail.Repositories;
using JWTRegen.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CheckingSupplierEmail.Controllers
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

                Response.Cookies.Append("purvenportal_jwt", token, new CookieOptions
                {
                    HttpOnly = false,
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
            await _emp.Login(form.txt_empno, form.txt_password);
            vw_emp obj_emp = await _emp.GetByEmpno(form.txt_empno);
            if(obj_emp.empstatusno == "R")
            {
                ModelState.AddModelError("txt_empno", "Username is already resigned.");
                return true;
            }
            var allowedDepartments = new List<string> { "PCM", "ISM" };
            if (!allowedDepartments.Contains(obj_emp.departmentno))
            {
                ModelState.AddModelError("txt_empno", "You do not have permission to access the system.");
                return true;
            }
            return result;
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            Response.Cookies.Append("purvenportal_jwt", string.Empty, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Strict,
                Path = "/", // Set cookie available across the entire site
                Expires = DateTime.UtcNow.AddDays(-1)
            });
            return RedirectToAction(nameof(vLogin));
        }
    }
}
