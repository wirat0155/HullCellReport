using CheckingSupplierEmail.Models.DbViewModels;
using CheckingSupplierEmail.Repositories;
using JWTRegen.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace CheckingSupplierEmail.Controllers
{
    [Authorize]
    public class EmailController : BaseController
    {
        private readonly PurCCEmailRepository _email;
        private readonly EmployeeRepository _emp;
        private readonly IClaimsHelper _claimsHelper;

        public EmailController(
            PurCCEmailRepository email,
            EmployeeRepository emp,
            JWTRegen.Interfaces.IClaimsHelper claimsHelper)
        {
            _email = email;
            _emp = emp;
            _claimsHelper = claimsHelper;
        }

        public class vSetEmailVM
        {
            public List<vw_PUR_CCEmail> ls_data { get; set; } = new List<vw_PUR_CCEmail>();
        }
        public async Task<IActionResult> vSetEmail()
        {
            vSetEmailVM model = new vSetEmailVM();
            var rs_data = await _email.GetAll();
            model.ls_data = rs_data.ToList();
            return View(model);
        }

        public class SetUsernameFM
        {
            public string txt_oldusername { get; set; }
            public string txt_newusername { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> SetUsername(SetUsernameFM form)
        {
            try
            {
                if(await ValSetUsername(form))
                {
                    return GenerateErrorResponse();
                }
                string txt_user = _claimsHelper.GetUserId(User);
                await _email.UpdateUsername(form.txt_oldusername, form.txt_newusername, txt_user);
                var obj_emp = await _emp.GetByEmpno(form.txt_newusername);
                string empnameeng = obj_emp.empnameeng;
                return Json(new { success = true, text = "Update successfully.", empnameeng });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    text = $"An unexpected error occurred: {ex.Message} {ex.InnerException?.Message ?? ""}"
                });
            }
        }

        private async Task<bool> ValSetUsername(SetUsernameFM form)
        {
            bool result = false;
            #region required
            if (string.IsNullOrEmpty(form.txt_newusername))
            {
                ModelState.AddModelError("txt_newusername", "Username is required.");
                return true;
            }
            #endregion

            #region found empno and not resign
            var obj_emp = await _emp.GetByEmpno(form.txt_newusername);
            if (obj_emp == null)
            {
                ModelState.AddModelError("txt_newusername", "Username is not found.");
                return true;
            }
            else if (obj_emp.empstatusno == "R")
            {
                ModelState.AddModelError("txt_newusername", "Username is resigned.");
                return true;
            }
            #endregion
            return result;
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUsername(string txt_username)
        {
            try
            {
                string txt_user = _claimsHelper.GetUserId(User);
                await _email.RemoveUsername(txt_username, txt_user);
                return Json(new { success = true, text = "Remove successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    text = $"An unexpected error occurred: {ex.Message} {ex.InnerException?.Message ?? ""}"
                });
            }
        }

        public class AddEmailFM
        {
            public int txt_id { get; set; }
            [Required(ErrorMessage = "Email is required.")]
            [EmailAddress(ErrorMessage = "Email is invalid.")]
            public string txt_email { get; set; }
            [Required(ErrorMessage = "Username is required.")]
            public string txt_username { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> AddEmail(AddEmailFM form)
        {   
            try
            {
                if (await ValAddEmail(form))
                {
                    return GenerateErrorResponse();
                }
                string txt_user = _claimsHelper.GetUserId(User);
                await _email.Add(form.txt_username, "EMAIL", form.txt_email, txt_user);
                return Json(new { success = true, text = "Add email successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    text = $"An unexpected error occurred: {ex.Message} {ex.InnerException?.Message ?? ""}"
                });
            }
        }

        private async Task<bool> ValAddEmail(AddEmailFM form)
        {
            bool result = false;
            if (!TryValidateModel(form))
            {
                return true;
            }

            var emailRegex = @"^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$";
            if (!System.Text.RegularExpressions.Regex.IsMatch(form.txt_email, emailRegex))
            {
                ModelState.AddModelError("txt_email", "Email is invalid.");
                return true;
            }

            #region Validate Duplicate
            if (await _email.CheckDuplicateEmail(form.txt_username, form.txt_email, form.txt_id))
            {
                ModelState.AddModelError("txt_email", "Email is duplicated.");
                return true;
            }
            #endregion

            return result;
        }

        public class UpdateEmailFM
        {
            public int txt_id { get; set; }

            [Required(ErrorMessage = "Email is required.")]
            [EmailAddress(ErrorMessage = "Email is invalid.")]
            public string txt_email { get; set; }
        }


        [HttpPost]
        public async Task<IActionResult> UpdateEmail(UpdateEmailFM form)
        {
            try
            {
                if (await ValUpdateEmail(form))
                {
                    return GenerateErrorResponse();
                }

                string txt_user = _claimsHelper.GetUserId(User);
                await _email.Update(form.txt_id, form.txt_email, txt_user);

                return Json(new { success = true, text = "Email updated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    text = $"An unexpected error occurred: {ex.Message} {ex.InnerException?.Message ?? ""}"
                });
            }
        }

        private async Task<bool> ValUpdateEmail(UpdateEmailFM form)
        {
            bool result = false;

            // ตรวจ model validation (เช่น Required, EmailAddress)
            if (!TryValidateModel(form))
                return true;

            // ตรวจ format email
            var emailRegex = @"^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$";
            if (!System.Text.RegularExpressions.Regex.IsMatch(form.txt_email, emailRegex))
            {
                ModelState.AddModelError("txt_email", "Email is invalid.");
                return true;
            }

            return result;
        }

        [HttpPost]
        public async Task<IActionResult> DeleteById(int txt_id)
        {
            try
            {
                string txt_user = _claimsHelper.GetUserId(User);
                await _email.Delete(txt_id, txt_user);
                return Json(new { success = true, text = "Remove email successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    text = $"An unexpected error occurred: {ex.Message} {ex.InnerException?.Message ?? ""}"
                });
            }
        }
        public class UpdateEmpnoFM
        {
            public int txt_id { get; set; }

            [Required(ErrorMessage = "Empno is required.")]
            public string txt_empno { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateEmpno(UpdateEmpnoFM form)
        {
            try
            {
                if (await ValUpdateEmpno(form))
                {
                    return GenerateErrorResponse();
                }

                string txt_user = _claimsHelper.GetUserId(User);
                await _email.Update(form.txt_id, form.txt_empno, txt_user);

                return Json(new { success = true, text = "Empno updated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    text = $"An unexpected error occurred: {ex.Message} {ex.InnerException?.Message ?? ""}"
                });
            }
        }
        private async Task<bool> ValUpdateEmpno(UpdateEmpnoFM form)
        {
            bool result = false;

            // ตรวจ model validation (เช่น Required, EmailAddress)
            if (!TryValidateModel(form))
                return true;

            // ตรวจว่ามีรหัสพนักงานนี้จริง และยังไม่ลาออก
            var obj_emp = await _emp.GetByEmpno(form.txt_empno);
            if (obj_emp == null)
            {
                ModelState.AddModelError("txt_empno", "Empno is not found.");
                return true;
            }
            if(obj_emp.empstatusno == "R")
            {
                ModelState.AddModelError("txt_empno", "Empno is already resigned.");
                return true;
            }

            return result;
        }

        public class AddEmpnoFM
        {
            public int txt_id { get; set; }
            [Required(ErrorMessage = "Empno is required.")]
            public string txt_empno { get; set; }
            [Required(ErrorMessage = "Username is required.")]
            public string txt_username { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> AddEmpno(AddEmpnoFM form)
        {
            try
            {
                if (await ValAddEmpno(form))
                {
                    return GenerateErrorResponse();
                }
                string txt_user = _claimsHelper.GetUserId(User);
                await _email.Add(form.txt_username, "EMPNO", form.txt_empno, txt_user);
                return Json(new { success = true, text = "Add empno successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    text = $"An unexpected error occurred: {ex.Message} {ex.InnerException?.Message ?? ""}"
                });
            }
        }

        private async Task<bool> ValAddEmpno(AddEmpnoFM form)
        {
            bool result = false;
            if (!TryValidateModel(form))
            {
                return true;
            }

            // ตรวจว่ามีรหัสพนักงานนี้จริง และยังไม่ลาออก
            var obj_emp = await _emp.GetByEmpno(form.txt_empno);
            if (obj_emp == null)
            {
                ModelState.AddModelError("txt_empno", "Empno is not found.");
                return true;
            }
            if (obj_emp.empstatusno == "R")
            {
                ModelState.AddModelError("txt_empno", "Empno is already resigned.");
                return true;
            }

            return result;
        }
    }
}
