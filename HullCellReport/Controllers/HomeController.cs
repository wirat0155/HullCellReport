using HullCellReport.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using JWTRegen.Interfaces;
using Microsoft.AspNetCore.Authorization;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using DinkToPdf;
using DinkToPdf.Contracts;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Net;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;

namespace HullCellReport.Controllers
{
    [Authorize]
    public class HomeController : BaseController
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly IClaimsHelper _claimsHelper;
        private readonly Repositories.EmployeeRepository _employeeRepo;
        private readonly IConverter _pdfConverter;
        private readonly IConfiguration _configuration;

        public HomeController(
            ILogger<HomeController> logger, 
        IWebHostEnvironment env,
        JWTRegen.Interfaces.IClaimsHelper claimsHelper,
        Repositories.EmployeeRepository employeeRepo,
        IConverter pdfConverter,
        IConfiguration configuration)
        {
            _logger = logger;
            _env = env;
            _claimsHelper = claimsHelper;
            _employeeRepo = employeeRepo;
            _pdfConverter = pdfConverter;
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult vDashboard()
        {
            return View();
        }

        public IActionResult vViewReport()
        {
            return View();
        }

        public async Task<IActionResult> vCreateReport(string uuid = null)
        {
            // If no UUID (creating new), check for incomplete reports
            if (string.IsNullOrEmpty(uuid))
            {
                var dataLogPath = Path.Combine(_env.WebRootPath, "data_log");
                
                if (Directory.Exists(dataLogPath))
                {
                    var jsonFiles = Directory.GetFiles(dataLogPath, "*.json");
                    
                    foreach (var filePath in jsonFiles)
                    {
                        var jsonContent = await System.IO.File.ReadAllTextAsync(filePath);
                        if (!string.IsNullOrWhiteSpace(jsonContent))
                        {
                            var reports = JsonSerializer.Deserialize<List<CreateReportFM>>(jsonContent);
                            var incompleteReport = reports?.FirstOrDefault(r => r.txt_status != "C");
                            
                            if (incompleteReport != null)
                            {
                                // Redirect to edit the incomplete report
                                TempData["ErrorMessage"] = "มีรายการที่ยังไม่ Complete กรุณา Complete รายการเดิมก่อนสร้างใหม่";
                                return RedirectToAction("vCreateReport", new { uuid = incompleteReport.txt_uuid });
                            }
                        }
                    }
                }
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateReport(CreateReportFM form, string deleted_images = "")
        {
            try
            {
                if (await ValCreateReport(form))
                {
                    return GenerateErrorResponse();
                }

                DateTime currentTime = DateTime.Now;
                string jwtUser = _claimsHelper.GetUserId(User) ?? "ADMIN";
                bool isUpdate = !string.IsNullOrEmpty(form.txt_uuid);

                var dataLogPath = Path.Combine(_env.WebRootPath, "data_log");
                var imagesPath = Path.Combine(_env.WebRootPath, "images", "data_log");
                
                // Create directories if not exist
                if (!Directory.Exists(dataLogPath))
                {
                    Directory.CreateDirectory(dataLogPath);
                }
                if (!Directory.Exists(imagesPath))
                {
                    Directory.CreateDirectory(imagesPath);
                }

                // Handle image uploads
                var uploadedImageNames = new List<string>();
                if (form.txt_file_upload != null && form.txt_file_upload.Count > 0)
                {
                    foreach (var file in form.txt_file_upload)
                    {
                        if (file.Length > 0)
                        {
                            // Get file extension
                            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                            
                            // Validate image extensions
                            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
                            if (!allowedExtensions.Contains(extension))
                            {
                                continue; // Skip non-image files
                            }

                            // Generate unique filename with UUID
                            var imageUuid = Guid.NewGuid().ToString();
                            var fileName = $"{imageUuid}{extension}";
                            var filePath = Path.Combine(imagesPath, fileName);

                            // Save image directly without compression
                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }

                            uploadedImageNames.Add(fileName);
                        }
                    }
                }

                if (isUpdate)
                {
                    // Handle deleted images
                    if (!string.IsNullOrEmpty(deleted_images))
                    {
                        var deletedList = deleted_images.Split(',', StringSplitOptions.RemoveEmptyEntries);
                        foreach (var imageName in deletedList)
                        {
                            var imageFilePath = Path.Combine(imagesPath, imageName.Trim());
                            if (System.IO.File.Exists(imageFilePath))
                            {
                                System.IO.File.Delete(imageFilePath);
                            }
                        }
                    }

                    // Get existing images and merge with new uploads
                    var jsonFiles = Directory.GetFiles(dataLogPath, "*.json");
                    foreach (var file in jsonFiles)
                    {
                        var existingJson = await System.IO.File.ReadAllTextAsync(file);
                        if (!string.IsNullOrWhiteSpace(existingJson))
                        {
                            var existingReports = JsonSerializer.Deserialize<List<CreateReportFM>>(existingJson);
                            var existingReport = existingReports?.FirstOrDefault(r => r.txt_uuid == form.txt_uuid);
                            
                            if (existingReport != null && existingReport.txt_uploaded_images != null)
                            {
                                // Remove deleted images from existing list
                                var deletedList = !string.IsNullOrEmpty(deleted_images) 
                                    ? deleted_images.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList()
                                    : new List<string>();
                                
                                var remainingImages = existingReport.txt_uploaded_images
                                    .Where(img => !deletedList.Contains(img))
                                    .ToList();
                                
                                // Merge with new uploads
                                remainingImages.AddRange(uploadedImageNames);
                                form.txt_uploaded_images = remainingImages;
                                break;
                            }
                        }
                    }
                }
                else
                {
                    // New report - just use uploaded images
                    form.txt_uploaded_images = uploadedImageNames;
                }

                if (isUpdate)
                {
                    // Update existing report
                    bool updated = false;
                    var jsonFiles = Directory.GetFiles(dataLogPath, "*.json");

                    foreach (var filePath in jsonFiles)
                    {
                        var existingJson = await System.IO.File.ReadAllTextAsync(filePath);
                        if (!string.IsNullOrWhiteSpace(existingJson))
                        {
                            var reports = JsonSerializer.Deserialize<List<CreateReportFM>>(existingJson) ?? new List<CreateReportFM>();
                            var existingReport = reports.FirstOrDefault(r => r.txt_uuid == form.txt_uuid);

                            if (existingReport != null)
                            {
                                // Check if report is already Complete
                                if (existingReport.txt_status == "C")
                                {
                                    return Json(new { success = false, errors = new[] { "ไม่สามารถแก้ไขรายการที่ Complete แล้ว" } });
                                }

                                // Update fields
                                var index = reports.IndexOf(existingReport);
                                form.txt_creuser = existingReport.txt_creuser;
                                form.txt_credate = existingReport.txt_credate;
                                form.txt_updateuser = jwtUser;
                                form.txt_updatedate = currentTime;
                                reports[index] = form;

                                // Save updated data
                                var options = new JsonSerializerOptions
                                {
                                    WriteIndented = true,
                                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                                };
                                var jsonString = JsonSerializer.Serialize(reports, options);
                                await System.IO.File.WriteAllTextAsync(filePath, jsonString);

                                // Send to ThingsBoard if status is Complete
                                if (form.txt_status == "C")
                                {
                                    await SendToThingsBoard(form);
                                    
                                    // Export PDF and save to File Server
                                    await ExportAndSavePDFToFileServer(form);
                                }

                                updated = true;
                                break;
                            }
                        }
                    }

                    if (!updated)
                    {
                        return Json(new { success = false, errors = new[] { "ไม่พบข้อมูลที่ต้องการแก้ไข" } });
                    }

                    return Json(new { success = true, text = "แก้ไขข้อมูลสำเร็จ", uuid = form.txt_uuid });
                }
                else
                {
                    // Check for incomplete reports before creating new
                    var jsonFiles = Directory.GetFiles(dataLogPath, "*.json");
                    
                    foreach (var file in jsonFiles)
                    {
                        var existingJson = await System.IO.File.ReadAllTextAsync(file);
                        if (!string.IsNullOrWhiteSpace(existingJson))
                        {
                            var existingReports = JsonSerializer.Deserialize<List<CreateReportFM>>(existingJson);
                            var incompleteReport = existingReports?.FirstOrDefault(r => r.txt_status != "C");
                            
                            if (incompleteReport != null)
                            {
                                return Json(new { 
                                    success = false, 
                                    errors = new[] { "มีรายการที่ยังไม่ Complete กรุณา Complete รายการเดิมก่อนสร้างใหม่" },
                                    redirectToEdit = true,
                                    uuid = incompleteReport.txt_uuid
                                });
                            }
                        }
                    }

                    // Create new report
                    form.txt_uuid = Guid.NewGuid().ToString();
                    form.txt_creuser = jwtUser;
                    form.txt_updateuser = jwtUser;
                    form.txt_credate = currentTime;
                    form.txt_updatedate = currentTime;

                    // Get current year
                    var currentYear = currentTime.Year;
                    var fileName = $"{currentYear}.json";
                    var filePath = Path.Combine(dataLogPath, fileName);

                    // Read existing data or create new list
                    List<CreateReportFM> reports = new List<CreateReportFM>();
                    if (System.IO.File.Exists(filePath))
                    {
                        var existingJson = await System.IO.File.ReadAllTextAsync(filePath);
                        if (!string.IsNullOrWhiteSpace(existingJson))
                        {
                            reports = JsonSerializer.Deserialize<List<CreateReportFM>>(existingJson) ?? new List<CreateReportFM>();
                        }
                    }

                    // Add new report
                    reports.Add(form);

                    // Save to JSON file
                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    };
                    var jsonString = JsonSerializer.Serialize(reports, options);
                    await System.IO.File.WriteAllTextAsync(filePath, jsonString);

                    // Send to ThingsBoard if status is Complete
                    if (form.txt_status == "C")
                    {
                        await SendToThingsBoard(form);
                        
                        // Export PDF and save to File Server
                        await ExportAndSavePDFToFileServer(form);
                    }

                    return Json(new { success = true, text = "บันทึกข้อมูลสำเร็จ", uuid = form.txt_uuid });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating report");
                return Json(new { success = false, errors = new[] { ex.Message } });
            }
        }

        private async Task SendToThingsBoard(CreateReportFM form)
        {
            try
            {
                var telemetryData = new List<Dictionary<string, object>>();

                // Process 208N
                var feed208n = ProcessAutoFeedData("208n", form.txt_auto_feed_208n, form.txt_uuid);
                telemetryData.Add(feed208n);

                // Process 208T
                var feed208t = ProcessAutoFeedData("208t", form.txt_auto_feed_208t, form.txt_uuid);
                telemetryData.Add(feed208t);

                // Process 208A
                var feed208a = ProcessAutoFeedData("208a", form.txt_auto_feed_208a, form.txt_uuid);
                telemetryData.Add(feed208a);

                // Process 208B
                var feed208b = ProcessAutoFeedData("208b", form.txt_auto_feed_208b, form.txt_uuid);
                telemetryData.Add(feed208b);

                // Send to ThingsBoard
                using (var httpClient = new System.Net.Http.HttpClient())
                {
                    var url = "http://thingsboard.cloud/api/v1/lP2VHpjA0nd9YCOHRRLL/telemetry";
                    var jsonContent = JsonSerializer.Serialize(telemetryData);
                    var content = new System.Net.Http.StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

                    var response = await httpClient.PostAsync(url, content);
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogWarning($"Failed to send data to ThingsBoard. Status: {response.StatusCode}");
                    }
                    else
                    {
                        _logger.LogInformation("Successfully sent data to ThingsBoard");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending data to ThingsBoard");
                // Don't throw - we don't want to fail the save operation if ThingsBoard fails
            }
        }

        private Dictionary<string, object> ProcessAutoFeedData(string feedType, string value, string uuid = null)
        {
            int status = 1; // Default: Open (1)
            int stopDurationH = 0;

            if (!string.IsNullOrEmpty(value))
            {
                // Check if it contains "Stop feed"
                if (value.Contains("Stop feed", StringComparison.OrdinalIgnoreCase))
                {
                    status = 0; // Stopped

                    // Extract duration hours using regex
                    var match = System.Text.RegularExpressions.Regex.Match(value, @"(\d+)\s*ชั่วโมง");
                    if (match.Success && int.TryParse(match.Groups[1].Value, out int hours))
                    {
                        stopDurationH = hours;
                    }
                }
                else if (value.Contains("Open", StringComparison.OrdinalIgnoreCase))
                {
                    status = 1; // Open
                }
            }

            // Create keys with prefix (e.g., auto_feed_208n_name, auto_feed_208n_status, etc.)
            var prefix = $"auto_feed_{feedType}";
            var result = new Dictionary<string, object>
            {
                { $"{prefix}_name", prefix },
                { $"{prefix}_status", status },
                { $"{prefix}_stop_duration_h", stopDurationH },
                { $"{prefix}_report_id", uuid ?? "" }
            };

            return result;
        }

        private async Task CompressAndSaveImage(Microsoft.AspNetCore.Http.IFormFile file, string outputPath)
        {
            using (var imageStream = file.OpenReadStream())
            using (var image = await Image.LoadAsync(imageStream))
            {
                // Resize to smaller size (max width/height: 1280px)
                int maxSize = 1280;
                if (image.Width > maxSize || image.Height > maxSize)
                {
                    if (image.Width > image.Height)
                    {
                        int newHeight = (int)((float)image.Height / image.Width * maxSize);
                        image.Mutate(x => x.Resize(maxSize, newHeight));
                    }
                    else
                    {
                        int newWidth = (int)((float)image.Width / image.Height * maxSize);
                        image.Mutate(x => x.Resize(newWidth, maxSize));
                    }
                }

                // Save as JPEG with quality 70 for smaller file size
                var encoder = new JpegEncoder
                {
                    Quality = 70
                };

                await image.SaveAsync(outputPath, encoder);
            }
        }

        private async Task<bool> ValCreateReport(CreateReportFM form){
            bool result = false;
            if (form.txt_status == "D")
            {
                return result;
            }

            if (string.IsNullOrEmpty(form.txt_line))
            {
                ModelState.AddModelError("txt_line", "กรุณาเลือกระหว่าง Zn-Ni1 และ Zn-Ni2");
                result = true;
            }
            if (string.IsNullOrEmpty(form.txt_analysis_by))
            {
                ModelState.AddModelError("txt_analysis_by", "กรุณากรอกรหัสพนักงาน");
                result = true;
            }
            else {
                var obj_emp = await _employeeRepo.GetByEmpno(form.txt_analysis_by.Trim().ToUpper(), true);
                if(obj_emp == null)
                {
                    ModelState.AddModelError("txt_analysis_by", "รหัสพนักงานไม่ถูกต้อง");
                    result = true;
                }
            }

            if (form.txt_sampling_date == null)
            {
                ModelState.AddModelError("txt_sampling_date", "กรุณากรอกค่า");
                result = true;
            }
            if (form.txt_time == null)
            {
                ModelState.AddModelError("txt_time", "กรุณากรอกค่า");
                result = true;
            }
            if (form.txt_zinc_metal == null)
            {
                ModelState.AddModelError("txt_zinc_metal", "กรุณากรอกค่า");
                result = true;
            }
            if (form.txt_caustic_soda == null)
            {
                ModelState.AddModelError("txt_caustic_soda", "กรุณากรอกค่า");
                result = true;
            }
            if (form.txt_sodium_carbonate == null)
            {
                ModelState.AddModelError("txt_sodium_carbonate", "กรุณากรอกค่า");
                result = true;
            }
            if (form.txt_nickel == null)
            {
                ModelState.AddModelError("txt_nickel", "กรุณากรอกค่า");
                result = true;
            }
            if (form.txt_us_208t == null)
            {
                ModelState.AddModelError("txt_us_208t", "กรุณากรอกค่า");
                result = true;
            }
            if (form.txt_result_1cm == null)
            {
                ModelState.AddModelError("txt_result_1cm", "กรุณากรอกค่า");
                result = true;
            }
            if (form.txt_result_3cm == null)
            {
                ModelState.AddModelError("txt_result_3cm", "กรุณากรอกค่า");
                result = true;
            }
            if (form.txt_result_5cm == null)
            {
                ModelState.AddModelError("txt_result_5cm", "กรุณากรอกค่า");
                result = true;
            }
            if (form.txt_result_7cm == null)
            {
                ModelState.AddModelError("txt_result_7cm", "กรุณากรอกค่า");
                result = true;
            }
            if (form.txt_result_9cm == null)
            {
                ModelState.AddModelError("txt_result_9cm", "กรุณากรอกค่า");
                result = true;
            }
            if (form.txt_result_19cm == null)
            {
                ModelState.AddModelError("txt_result_19cm", "กรุณากรอกค่า");
                result = true;
            }
            if (form.txt_zn_1cm == null)
            {
                ModelState.AddModelError("txt_zn_1cm", "กรุณากรอกค่า");
                result = true;
            }
            if (form.txt_zn_3cm == null)
            {
                ModelState.AddModelError("txt_zn_3cm", "กรุณากรอกค่า");
                result = true;
            }
            if (form.txt_zn_5cm == null)
            {
                ModelState.AddModelError("txt_zn_5cm", "กรุณากรอกค่า");
                result = true;
            }
            if (form.txt_zn_7cm == null)
            {
                ModelState.AddModelError("txt_zn_7cm", "กรุณากรอกค่า");
                result = true;
            }
            if (form.txt_zn_9cm == null)
            {
                ModelState.AddModelError("txt_zn_9cm", "กรุณากรอกค่า");
                result = true;
            }
            if (form.txt_ni_1cm == null)
            {
                ModelState.AddModelError("txt_ni_1cm", "กรุณากรอกค่า");
                result = true;
            }
            if (form.txt_ni_3cm == null)
            {
                ModelState.AddModelError("txt_ni_3cm", "กรุณากรอกค่า");
                result = true;
            }
            if (form.txt_ni_5cm == null)
            {
                ModelState.AddModelError("txt_ni_5cm", "กรุณากรอกค่า");
                result = true;
            }
            if (form.txt_ni_7cm == null)
            {
                ModelState.AddModelError("txt_ni_7cm", "กรุณากรอกค่า");
                result = true;
            }
            if (form.txt_ni_9cm == null)
            {
                ModelState.AddModelError("txt_ni_9cm", "กรุณากรอกค่า");
                result = true;
            }

            return result;
        }

        [HttpGet]
        public async Task<IActionResult> CheckIncompleteReports()
        {
            try
            {
                var dataLogPath = Path.Combine(_env.WebRootPath, "data_log");
                
                if (Directory.Exists(dataLogPath))
                {
                    var jsonFiles = Directory.GetFiles(dataLogPath, "*.json");
                    
                    foreach (var filePath in jsonFiles)
                    {
                        var jsonContent = await System.IO.File.ReadAllTextAsync(filePath);
                        if (!string.IsNullOrWhiteSpace(jsonContent))
                        {
                            var reports = JsonSerializer.Deserialize<List<CreateReportFM>>(jsonContent);
                            var incompleteReport = reports?.FirstOrDefault(r => r.txt_status != "C");
                            
                            if (incompleteReport != null)
                            {
                                return Json(new { 
                                    success = true, 
                                    hasIncomplete = true,
                                    uuid = incompleteReport.txt_uuid,
                                    message = "มีรายการที่ยังไม่ Complete กรุณา Complete รายการเดิมก่อนสร้างใหม่"
                                });
                            }
                        }
                    }
                }

                return Json(new { success = true, hasIncomplete = false });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking incomplete reports");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteReport(string uuid)
        {
            try
            {
                if (string.IsNullOrEmpty(uuid))
                {
                    return Json(new { success = false, message = "UUID is required" });
                }

                var dataLogPath = Path.Combine(_env.WebRootPath, "data_log");
                bool deleted = false;

                if (Directory.Exists(dataLogPath))
                {
                    var jsonFiles = Directory.GetFiles(dataLogPath, "*.json");

                    foreach (var filePath in jsonFiles)
                    {
                        var jsonContent = await System.IO.File.ReadAllTextAsync(filePath);
                        if (!string.IsNullOrWhiteSpace(jsonContent))
                        {
                            var reports = JsonSerializer.Deserialize<List<CreateReportFM>>(jsonContent);
                            var reportToDelete = reports?.FirstOrDefault(r => r.txt_uuid == uuid);

                            if (reportToDelete != null)
                            {
                                // Check if report is Complete
                                if (reportToDelete.txt_status == "C")
                                {
                                    return Json(new { success = false, message = "ไม่สามารถลบรายการที่ Complete แล้ว" });
                                }

                                // Delete associated images
                                if (reportToDelete.txt_uploaded_images != null && reportToDelete.txt_uploaded_images.Count > 0)
                                {
                                    var imagesPath = Path.Combine(_env.WebRootPath, "images", "data_log");
                                    foreach (var imageName in reportToDelete.txt_uploaded_images)
                                    {
                                        var imageFilePath = Path.Combine(imagesPath, imageName);
                                        if (System.IO.File.Exists(imageFilePath))
                                        {
                                            try
                                            {
                                                System.IO.File.Delete(imageFilePath);
                                            }
                                            catch (Exception ex)
                                            {
                                                _logger.LogWarning(ex, $"Failed to delete image: {imageName}");
                                            }
                                        }
                                    }
                                }

                                // Remove the report
                                reports.Remove(reportToDelete);

                                // Save updated data
                                var options = new JsonSerializerOptions
                                {
                                    WriteIndented = true,
                                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                                };
                                var jsonString = JsonSerializer.Serialize(reports, options);
                                await System.IO.File.WriteAllTextAsync(filePath, jsonString);

                                deleted = true;
                                break;
                            }
                        }
                    }
                }

                if (deleted)
                {
                    return Json(new { success = true, message = "ลบรายการสำเร็จ" });
                }
                else
                {
                    return Json(new { success = false, message = "ไม่พบรายการที่ต้องการลบ" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting report");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetReportByUuid(string uuid)
        {
            try
            {
                if (string.IsNullOrEmpty(uuid))
                {
                    return Json(new { success = false, message = "UUID is required" });
                }

                var dataLogPath = Path.Combine(_env.WebRootPath, "data_log");
                
                // Search through all JSON files
                if (Directory.Exists(dataLogPath))
                {
                    var jsonFiles = Directory.GetFiles(dataLogPath, "*.json");
                    
                    foreach (var filePath in jsonFiles)
                    {
                        var jsonContent = await System.IO.File.ReadAllTextAsync(filePath);
                        if (!string.IsNullOrWhiteSpace(jsonContent))
                        {
                            var reports = JsonSerializer.Deserialize<List<CreateReportFM>>(jsonContent);
                            var report = reports?.FirstOrDefault(r => r.txt_uuid == uuid);
                            
                            if (report != null)
                            {
                                return Json(new { success = true, data = report });
                            }
                        }
                    }
                }

                return Json(new { success = false, message = "Report not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting report by UUID");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetHullCellReports(
            int page = 1, 
            int pageSize = 10, 
            string sortOrder = "desc",
            string startDate = "",
            string endDate = "",
            string status = "")
        {
            try
            {
                var allReports = new List<CreateReportFM>();
                var dataLogPath = Path.Combine(_env.WebRootPath, "data_log");

                // Determine which years to read based on date range
                var yearsToRead = new HashSet<int>();
                
                if (!string.IsNullOrEmpty(startDate) && DateTime.TryParse(startDate, out DateTime start))
                {
                    yearsToRead.Add(start.Year);
                }
                
                if (!string.IsNullOrEmpty(endDate) && DateTime.TryParse(endDate, out DateTime end))
                {
                    yearsToRead.Add(end.Year);
                    
                    // Add all years between start and end
                    if (!string.IsNullOrEmpty(startDate) && DateTime.TryParse(startDate, out DateTime startYear))
                    {
                        for (int year = startYear.Year; year <= end.Year; year++)
                        {
                            yearsToRead.Add(year);
                        }
                    }
                }
                
                // If no date range specified, use current year
                if (yearsToRead.Count == 0)
                {
                    yearsToRead.Add(DateTime.Now.Year);
                }

                // Read data from JSON files
                foreach (var year in yearsToRead)
                {
                    var fileName = $"{year}.json";
                    var filePath = Path.Combine(dataLogPath, fileName);

                    if (System.IO.File.Exists(filePath))
                    {
                        var jsonContent = await System.IO.File.ReadAllTextAsync(filePath);
                        if (!string.IsNullOrWhiteSpace(jsonContent))
                        {
                            var reports = JsonSerializer.Deserialize<List<CreateReportFM>>(jsonContent);
                            if (reports != null && reports.Count > 0)
                            {
                                allReports.AddRange(reports);
                            }
                        }
                    }
                }

                // Get unique empnos and fetch employee names in one query
                var uniqueEmpnos = allReports
                    .Where(r => !string.IsNullOrEmpty(r.txt_analysis_by))
                    .Select(r => r.txt_analysis_by)
                    .Distinct()
                    .ToList();

                var empNameMap = await _employeeRepo.GetEmployeeNamesByEmpnos(uniqueEmpnos);

                // Transform to display format
                var transformedData = allReports.Select(r => new
                {
                    id = r.txt_uuid,
                    createdDate = r.txt_credate,
                    createdBy = r.txt_analysis_by,
                    createdByName = empNameMap.ContainsKey(r.txt_analysis_by ?? "") ? empNameMap[r.txt_analysis_by] : "",
                    tank208N = r.txt_auto_feed_208n ?? "",
                    tank208T = r.txt_auto_feed_208t ?? "",
                    tank208A = r.txt_auto_feed_208a ?? "",
                    tank208B = r.txt_auto_feed_208b ?? "",
                    status = r.txt_status ?? ""
                }).ToList();

                // Apply filters
                IEnumerable<dynamic> filteredData = transformedData;

                // Filter by date range
                if (!string.IsNullOrEmpty(startDate) && DateTime.TryParse(startDate, out DateTime startFilter))
                {
                    filteredData = filteredData.Where(x => x.createdDate >= startFilter);
                }

                if (!string.IsNullOrEmpty(endDate) && DateTime.TryParse(endDate, out DateTime endFilter))
                {
                    endFilter = endFilter.AddDays(1).AddSeconds(-1); // Include the entire end date
                    filteredData = filteredData.Where(x => x.createdDate <= endFilter);
                }

                // Filter by status
                if (!string.IsNullOrEmpty(status))
                {
                    filteredData = filteredData.Where(x => x.status == status);
                }

                // Apply sorting
                if (sortOrder == "asc")
                {
                    filteredData = filteredData.OrderBy(x => x.createdDate);
                }
                else
                {
                    filteredData = filteredData.OrderByDescending(x => x.createdDate);
                }

                var sortedData = filteredData.ToList();
                
                // Pagination
                var total = sortedData.Count;
                var pagedData = sortedData.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                // Count today's sampling (always use Bangkok time)
                var bangkokTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
                var todayBangkok = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, bangkokTimeZone).Date;
                
                int todaySamplingCount = allReports.Count(r => 
                    !string.IsNullOrEmpty(r.txt_sampling_date) && 
                    DateTime.TryParse(r.txt_sampling_date, out DateTime samplingDate) &&
                    samplingDate.Date == todayBangkok);

                var todayDateString = todayBangkok.ToString("dd/MM/yyyy", new System.Globalization.CultureInfo("th-TH"));

                return Json(new { success = true, data = pagedData, total, todaySamplingCount, todayDate = todayDateString });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting hull cell reports");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCurrentUserInfo()
        {
            try
            {
                var username = _claimsHelper.GetUserId(User) ?? "ADMIN";
                if (string.IsNullOrEmpty(username))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var employee = await _employeeRepo.GetByEmpno(username, true);
                
                if (employee != null)
                {
                    return Json(new { 
                        success = true, 
                        empno = employee.empno, 
                        empnameeng = employee.empnameeng 
                    });
                }
                else
                {
                    return Json(new { success = false, message = "Employee not found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user info");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetEmployeeByEmpno(string empno)
        {
            try
            {
                if (string.IsNullOrEmpty(empno) || empno.Length < 6)
                {
                    return Json(new { success = false, message = "Employee number must be at least 6 characters" });
                }

                var employee = await _employeeRepo.GetByEmpno(empno, true);
                
                if (employee != null && !string.IsNullOrEmpty(employee.empnameeng))
                {
                    return Json(new { success = true, empnameeng = employee.empnameeng });
                }
                else
                {
                    return Json(new { success = false, message = "Employee not found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting employee");
                return Json(new { success = false, message = "Employee not found" });
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ExportPDF(string uuid)
        {
            try
            {
                if (string.IsNullOrEmpty(uuid))
                {
                    return BadRequest("UUID is required");
                }

                var dataLogPath = Path.Combine(_env.WebRootPath, "data_log");
                CreateReportFM report = null;

                if (Directory.Exists(dataLogPath))
                {
                    var jsonFiles = Directory.GetFiles(dataLogPath, "*.json");
                    foreach (var filePath in jsonFiles)
                    {
                        var jsonContent = await System.IO.File.ReadAllTextAsync(filePath);
                        if (!string.IsNullOrWhiteSpace(jsonContent))
                        {
                            var reports = JsonSerializer.Deserialize<List<CreateReportFM>>(jsonContent);
                            report = reports?.FirstOrDefault(r => r.txt_uuid == uuid);
                            if (report != null) break;
                        }
                    }
                }

                if (report == null)
                {
                    return NotFound("Report not found");
                }

                // Render view to string
                var viewPath = "~/Views/PDFTemplate/Report.cshtml";
                var htmlContent = await RenderViewToStringAsync(viewPath, report);

                // Convert logo to base64
                var logoPath = Path.Combine(_env.WebRootPath, "images", "system", "report_logo.jpg");
                if (System.IO.File.Exists(logoPath))
                {
                    var logoBytes = await System.IO.File.ReadAllBytesAsync(logoPath);
                    var logoBase64 = Convert.ToBase64String(logoBytes);
                    var logoBase64Src = $"data:image/jpeg;base64,{logoBase64}";
                    htmlContent = htmlContent.Replace("/images/system/report_logo.jpg", logoBase64Src);
                }

                // Convert image paths to base64 for PDF and rotate if landscape
                if (report.txt_uploaded_images != null && report.txt_uploaded_images.Count > 0)
                {
                    var imagesPath = Path.Combine(_env.WebRootPath, "images", "data_log");
                    foreach (var imageName in report.txt_uploaded_images)
                    {
                        var imagePath = Path.Combine(imagesPath, imageName);
                        if (System.IO.File.Exists(imagePath))
                        {
                            using (var image = Image.Load(imagePath))
                            {
                                // Rotate if landscape
                                if (image.Width > image.Height)
                                {
                                    image.Mutate(x => x.Rotate(90));
                                }
                                
                                // Convert to base64
                                using (var ms = new MemoryStream())
                                {
                                    await image.SaveAsJpegAsync(ms);
                                    var base64 = Convert.ToBase64String(ms.ToArray());
                                    var base64Src = $"data:image/jpeg;base64,{base64}";
                                    
                                    // Replace the path with base64
                                    var pathToReplace = $"/images/data_log/{imageName}";
                                    htmlContent = htmlContent.Replace(pathToReplace, base64Src);
                                }
                            }
                        }
                    }
                }

                // Generate PDF
                var doc = new HtmlToPdfDocument()
                {
                    GlobalSettings = {
                        ColorMode = ColorMode.Color,
                        Orientation = Orientation.Portrait,
                        PaperSize = PaperKind.A4,
                    },
                    Objects = {
                        new ObjectSettings() {
                            HtmlContent = htmlContent,
                            WebSettings = { DefaultEncoding = "utf-8" }
                        }
                    }
                };

                var pdf = _pdfConverter.Convert(doc);
                
                // Format date and time for filename
                string dateStr = "Unknown";
                if (!string.IsNullOrEmpty(report.txt_sampling_date) && DateTime.TryParse(report.txt_sampling_date, out DateTime samplingDate))
                {
                    dateStr = samplingDate.ToString("yyyyMMdd");
                }
                
                string timeStr = "";
                if (!string.IsNullOrEmpty(report.txt_time) && TimeSpan.TryParse(report.txt_time, out TimeSpan samplingTime))
                {
                    timeStr = $"_{samplingTime.Hours:D2}{samplingTime.Minutes:D2}";
                }
                
                var fileName = $"HullCellReport_{dateStr}{timeStr}_{uuid.Substring(0, 8)}.pdf";

                return File(pdf, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting PDF");
                return StatusCode(500, "Error generating PDF");
            }
        }

        private async Task<string> RenderViewToStringAsync(string viewPath, object model)
        {
            ViewData.Model = model;
            using (var sw = new StringWriter())
            {
                var engine = HttpContext.RequestServices.GetService(typeof(Microsoft.AspNetCore.Mvc.ViewEngines.ICompositeViewEngine)) as Microsoft.AspNetCore.Mvc.ViewEngines.ICompositeViewEngine;
                var viewResult = engine.GetView(null, viewPath, false);

                if (!viewResult.Success)
                {
                    throw new InvalidOperationException($"View {viewPath} not found");
                }

                var viewContext = new ViewContext(
                    ControllerContext,
                    viewResult.View,
                    ViewData,
                    TempData,
                    sw,
                    new Microsoft.AspNetCore.Mvc.ViewFeatures.HtmlHelperOptions()
                );

                await viewResult.View.RenderAsync(viewContext);
                return sw.ToString();
            }
        }

        private async Task ExportAndSavePDFToFileServer(CreateReportFM report)
        {
            try
            {
                // Render view to string
                var viewPath = "~/Views/PDFTemplate/Report.cshtml";
                var htmlContent = await RenderViewToStringAsync(viewPath, report);

                // Convert logo to base64
                var logoPath = Path.Combine(_env.WebRootPath, "images", "system", "report_logo.jpg");
                if (System.IO.File.Exists(logoPath))
                {
                    var logoBytes = await System.IO.File.ReadAllBytesAsync(logoPath);
                    var logoBase64 = Convert.ToBase64String(logoBytes);
                    var logoBase64Src = $"data:image/jpeg;base64,{logoBase64}";
                    htmlContent = htmlContent.Replace("/images/system/report_logo.jpg", logoBase64Src);
                }

                // Convert image paths to base64 for PDF and rotate if landscape
                if (report.txt_uploaded_images != null && report.txt_uploaded_images.Count > 0)
                {
                    var imagesPath = Path.Combine(_env.WebRootPath, "images", "data_log");
                    foreach (var imageName in report.txt_uploaded_images)
                    {
                        var imagePath = Path.Combine(imagesPath, imageName);
                        if (System.IO.File.Exists(imagePath))
                        {
                            using (var image = Image.Load(imagePath))
                            {
                                // Rotate if landscape
                                if (image.Width > image.Height)
                                {
                                    image.Mutate(x => x.Rotate(90));
                                }
                                
                                // Convert to base64
                                using (var ms = new MemoryStream())
                                {
                                    await image.SaveAsJpegAsync(ms);
                                    var base64 = Convert.ToBase64String(ms.ToArray());
                                    var base64Src = $"data:image/jpeg;base64,{base64}";
                                    
                                    // Replace the path with base64
                                    var pathToReplace = $"/images/data_log/{imageName}";
                                    htmlContent = htmlContent.Replace(pathToReplace, base64Src);
                                }
                            }
                        }
                    }
                }

                // Generate PDF
                var doc = new HtmlToPdfDocument()
                {
                    GlobalSettings = {
                        ColorMode = ColorMode.Color,
                        Orientation = Orientation.Portrait,
                        PaperSize = PaperKind.A4,
                    },
                    Objects = {
                        new ObjectSettings() {
                            HtmlContent = htmlContent,
                            WebSettings = { DefaultEncoding = "utf-8" }
                        }
                    }
                };

                var pdfBytes = _pdfConverter.Convert(doc);
                
                // Format date and time for filename (same as ExportPDF method)
                string dateStr = "Unknown";
                if (!string.IsNullOrEmpty(report.txt_sampling_date) && DateTime.TryParse(report.txt_sampling_date, out DateTime samplingDate))
                {
                    dateStr = samplingDate.ToString("yyyyMMdd");
                }
                
                string timeStr = "";
                if (!string.IsNullOrEmpty(report.txt_time) && TimeSpan.TryParse(report.txt_time, out TimeSpan samplingTime))
                {
                    timeStr = $"_{samplingTime.Hours:D2}{samplingTime.Minutes:D2}";
                }
                
                var fileName = $"HullCellReport_{dateStr}{timeStr}_{report.txt_uuid.Substring(0, 8)}.pdf";

                // Save to File Server via SMB
                var credential = new NetworkCredential(_configuration["SMB:Username"], _configuration["SMB:Password"]);
                var sharePath = @"\\fileserver\File_ShareStorage\Share_WebStorage\HullCellReport";
                
                using (new WindowsNetworkFileShare(sharePath, credential))
                {
                    var fullPath = Path.Combine(sharePath, fileName);
                    await System.IO.File.WriteAllBytesAsync(fullPath, pdfBytes);
                    _logger.LogInformation($"PDF saved to file server: {fullPath}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting and saving PDF to file server");
                // Don't throw - we don't want to fail the save operation if PDF export fails
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }

    // Helper class for SMB network share access
    public class WindowsNetworkFileShare : IDisposable
    {
        [DllImport("mpr.dll")]
        private static extern int WNetAddConnection2(NetResource netResource, string password, string username, int flags);

        [DllImport("mpr.dll")]
        private static extern int WNetCancelConnection2(string name, int flags, bool force);

        [StructLayout(LayoutKind.Sequential)]
        private class NetResource
        {
            public int Scope = 0;
            public int Type = 1; // RESOURCETYPE_DISK
            public int DisplayType = 0;
            public int Usage = 0;
            public string LocalName = null;
            public string RemoteName = null;
            public string Comment = null;
            public string Provider = null;
        }

        private string _networkName;

        public WindowsNetworkFileShare(string networkName, NetworkCredential credentials)
        {
            _networkName = networkName;

            var netResource = new NetResource
            {
                Scope = 0,
                Type = 1,
                DisplayType = 0,
                Usage = 0,
                RemoteName = networkName
            };

            var userName = string.IsNullOrEmpty(credentials.Domain)
                ? credentials.UserName
                : credentials.Domain + "\\" + credentials.UserName;

            var result = WNetAddConnection2(netResource, credentials.Password, userName, 0);

            if (result != 0)
            {
                throw new System.ComponentModel.Win32Exception(result, $"Error connecting to network share: {networkName}");
            }
        }

        public void Dispose()
        {
            WNetCancelConnection2(_networkName, 0, true);
        }
    }
}
