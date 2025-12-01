# Export PDF Feature Setup

## สิ่งที่เพิ่มเข้ามา

### 1. Packages
- DinkToPdf
- DinkToPdf.Standard

### 2. Files ที่สร้างใหม่
- `Views/PDFTemplate/Report.cshtml` - PDF template แบบง่าย
- `wkhtmltopdf/libwkhtmltox.dll` - Native library สำหรับ PDF generation
- `wkhtmltopdf/README.txt` - คำแนะนำการติดตั้ง

### 3. Files ที่แก้ไข
- `Controllers/HomeController.cs` - เพิ่ม ExportPDF action และ RenderViewToStringAsync method
- `Startup.cs` - Configure DinkToPdf service และ load native library
- `Views/Home/vDashboard.cshtml` - เพิ่มคอลัมน์ Export และปุ่ม Export
- `wwwroot/js/Home/vDashboard.js` - เพิ่มฟังก์ชัน exportPDF()
- `Models/CreateReportFM.cs` - เพิ่ม txt_remark field
- `HullCellReport.csproj` - เพิ่ม copy DLL to output directory

### 4. ฟีเจอร์
- ปุ่ม Export PDF จะแสดงเฉพาะรายการที่มีสถานะ Complete (C)
- PDF จะแสดงข้อมูลพื้นฐานของ Hull Cell Report
- ชื่อไฟล์: HullCellReport_YYYYMMDD_UUID.pdf

## การใช้งาน
1. ไปที่หน้า Dashboard
2. รายการที่ Complete แล้วจะมีปุ่ม "Export" สีเขียว
3. คลิกปุ่ม Export เพื่อดาวน์โหลด PDF

## หมายเหตุ
- Template PDF ยังเป็นแบบง่ายๆ สามารถปรับแต่งเพิ่มเติมได้ที่ `Views/PDFTemplate/Report.cshtml`
- Native library (libwkhtmltox.dll) จะถูก copy ไปที่ bin folder อัตโนมัติเมื่อ build
