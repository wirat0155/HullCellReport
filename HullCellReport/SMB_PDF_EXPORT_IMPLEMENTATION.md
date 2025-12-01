# การเพิ่มฟีเจอร์ Export PDF และบันทึกไปที่ File Server ผ่าน SMB

## สิ่งที่เพิ่มเข้ามา

### 1. Using Statements ใหม่
- `System.Net` - สำหรับ NetworkCredential
- `System.Runtime.InteropServices` - สำหรับ DllImport และ Windows API

### 2. Dependency Injection
- เพิ่ม `IConfiguration` ใน constructor เพื่อเข้าถึง configuration settings

### 3. Method ใหม่
- `ExportAndSavePDFToFileServer(CreateReportFM report)` - Export PDF และบันทึกไปที่ File Server

### 4. Helper Class
- `WindowsNetworkFileShare` - จัดการการเชื่อมต่อกับ SMB network share
  - ใช้ Windows API (WNetAddConnection2, WNetCancelConnection2)
  - Implement IDisposable เพื่อ cleanup connection

## การทำงาน

เมื่อบันทึก Status Complete (txt_status == "C") ใน CreateReport method:
1. เรียก `SendToThingsBoard(form)` - ส่งข้อมูลไป ThingsBoard
2. เรียก `ExportAndSavePDFToFileServer(form)` - Export PDF และบันทึกไปที่ File Server

### ExportAndSavePDFToFileServer Method
1. Render view เป็น HTML string
2. แปลง logo และรูปภาพเป็น base64
3. Generate PDF จาก HTML
4. ตั้งชื่อไฟล์ตามรูปแบบเดียวกับ ExportPDF method:
   - Format: `HullCellReport_YYYYMMDD_HHMM_UUID.pdf`
   - ตัวอย่าง: `HullCellReport_20251201_1430_a1b2c3d4.pdf`
5. เชื่อมต่อกับ File Server ผ่าน SMB
6. บันทึกไฟล์ PDF

### WindowsNetworkFileShare Class
- เชื่อมต่อกับ network share โดยใช้ credentials จาก appsettings.json
- ใช้ `using` statement เพื่อ auto-disconnect เมื่อเสร็จสิ้น
- จัดการ error ด้วย Win32Exception

## Configuration

ใน `appsettings.json`:
```json
"SMB": {
  "Username": "web_storage",
  "Password": "Wr@zit2lFoS"
}
```

Network Share Path: `\\fileserver\File_ShareStorage\Share_WebStorage\HullCellReport`

## Error Handling

- ถ้า Export PDF หรือบันทึกไปที่ File Server ล้มเหลว จะ log error แต่ไม่ทำให้การบันทึกข้อมูลล้มเหลว
- ใช้ try-catch เพื่อป้องกันไม่ให้ผู้ใช้ได้รับ error เมื่อ PDF export ล้มเหลว

## หมายเหตุ

- ชื่อไฟล์ PDF ใช้รูปแบบเดียวกับ ExportPDF method เพื่อความสอดคล้อง
- PDF จะถูกบันทึกอัตโนมัติเมื่อ Status เป็น Complete
- ไม่จำเป็นต้องติดตั้ง NuGet package เพิ่มเติม เพราะใช้ Windows API โดยตรง
