using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MiniLoyalty.Data;

namespace MiniLoyalty.Controllers.Port;

// ===========================================================================================
// PORT 1:1 từ dự án nguồn 2022.7.Loyalty — FileController/UploadFile (#59).
//
// Nguồn tra cứu (chỉ đọc):
//   - Gate:     13.ClientGate/V10/idn.Loyalty.WebAPI/Controllers/FileController.cs:23-82
//   - Tiện ích: 13.ClientGate/V10/idn.Loyalty.WebAPI/Utils/FileUtils.cs:45-75
//   - Base:     13.ClientGate/V10/idn.Loyalty.WebAPI/Controllers/BaseController.cs:403-408
//   - Backend:  Nguồn không gọi backend Biz và không có bảng DB trong 2022.7.Loyalty.
//
// QUY TẮC NGHIỆP VỤ NGUỒN:
//   - Action UploadFile(HttpPostedFileWrapper file) nhận file qua multipart/form-data.
//   - Đọc header NetworkID, OrgID, Tid (:36-38) nhưng không dùng cho việc lưu file.
//   - Guard: if (file != null) (:41); ngược lại (:66) ném ServiceException("File null") (:68).
//   - Sinh mã file: string fileId = Guid.NewGuid().ToString("D"); (:43).
//   - Tên file: string fileName = file.FileName; (:44).
//   - Đuôi file chữ thường: var ext = System.IO.Path.GetExtension(fileName).ToLower(); (:46).
//   - Lưu file vật lý: FileUtils.SaveTempFile(file, fileId, ext); (:48) vào thư mục TempFiles.
//   - Đường dẫn ảo: filePath = ~/TempFiles/{fileId}{ext} (:49).
//   - URL đầy đủ: GetRequestBaseAddress() + filePath (:50).
//   - Kích thước file: double size = file.ContentLength; string filesize = size.ToString(); (:52-53).
//   - Trả kết quả: Success = true, Data gồm 6 thuộc tính:
//     Url, FilePath, AttFileId, FileName, FileSize, FlagFileUpload ("1").
// ===========================================================================================

/// <summary>
/// Controller xử lý upload file tạm — port 1:1 từ <c>FileController.cs</c> (#59).
/// Endpoint: <c>POST /api/file/uploadfile</c>.
/// </summary>
[ApiController]
[Route("api/file")]
public class FileControllerPort : ControllerBase
{
    private readonly IWebHostEnvironment _env;

    public FileControllerPort(IWebHostEnvironment env)
    {
        _env = env;
    }

    /// <summary>
    /// Upload file tạm lên thư mục TempFiles của máy chủ.
    /// Nguồn: <c>FileController.cs:23-82</c>
    /// </summary>
    /// <param name="file">File tải lên qua multipart/form-data</param>
    /// <returns>Thông tin file đã tải lên gồm Url, FilePath, AttFileId, FileName, FileSize, FlagFileUpload</returns>
    [HttpPost("uploadfile")]
    public async Task<IActionResult> UploadFile(IFormFile? file)
    {
        // Kiểm tra form file phụ trợ nếu tham số file chưa được bind trực tiếp (FileController.cs:41)
        if (file == null && Request.HasFormContentType && Request.Form.Files.Count > 0)
        {
            file = Request.Form.Files[0];
        }

        // Guard: if (file != null) (:41); ngược lại ném ServiceException("File null") (:66-68)
        // -> catch (:71-75) trả Success = false, lỗi "File null"
        if (file == null)
        {
            return PortResult.Fail("File_UploadFile", "Message", "File null");
        }

        try
        {
            // Sinh mã file duy nhất dạng Guid: string fileId = Guid.NewGuid().ToString("D"); (FileController.cs:43)
            string fileId = Guid.NewGuid().ToString("D");

            // Lấy tên file gốc: string fileName = file.FileName; (FileController.cs:44)
            string fileName = file.FileName;

            // Lấy phần mở rộng file và chuyển chữ thường: Path.GetExtension(fileName).ToLower(); (FileController.cs:46)
            string ext = Path.GetExtension(fileName).ToLower();

            // Xác định thư mục lưu trữ TempFiles trên máy chủ (FileUtils.cs:56-64)
            var webRoot = _env.WebRootPath;
            if (string.IsNullOrEmpty(webRoot))
            {
                webRoot = Path.Combine(_env.ContentRootPath, "wwwroot");
            }

            var tempDir = Path.Combine(webRoot, "TempFiles");
            // Đảm bảo thư mục tồn tại: CUtils.EnsureDirectoryExists (FileUtils.cs:59)
            if (!Directory.Exists(tempDir))
            {
                Directory.CreateDirectory(tempDir);
            }

            // Lưu file vật lý vào máy chủ: FileUtils.SaveTempFile(file, fileId, ext); (FileController.cs:48)
            var targetPath = Path.Combine(tempDir, fileId + ext);
            using (var stream = new FileStream(targetPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Đường dẫn ảo: FileUtils.GetTempFileVirtualPath(fileId, ext) -> ~/TempFiles/{fileId}{ext} (FileController.cs:49)
            var filePath = $"/TempFiles/{fileId}{ext}";   // Url.Content("~/TempFiles/..") (FileController.cs:49)

            // URL đầy đủ chứa Scheme, Host (kèm Port) và đường dẫn truy cập (FileController.cs:50, BaseController.cs:403-408)
            var url = PortExcel.GetRequestBaseAddress(Request) + filePath;   // GetRequestBaseAddress() + filePath (:50)

            // Kích thước file theo byte: double size = file.ContentLength; string filesize = size.ToString(); (FileController.cs:52-53)
            double size = file.Length;
            string filesize = size.ToString();

            // Trả kết quả thành công với 6 trường thuộc tính (FileController.cs:55-64)
            return Ok(new
            {
                Success = true,
                Data = new File_UploadFileResult
                {
                    Url = url,
                    FilePath = filePath,
                    AttFileId = fileId + ext,
                    FileName = fileName,
                    FileSize = filesize,
                    FlagFileUpload = "1" // 1: là file upload (FileController.cs:63)
                }
            });
        }
        catch (Exception ex)
        {
            // Bắt ngoại lệ và trả về Success = false theo khuôn PortResult (FileController.cs:71-75)
            return PortResult.Fail("File_UploadFile", "Exception", ex.Message);
        }
    }
}

/// <summary>
/// DTO dữ liệu trả về của tiện ích upload file — gồm 6 trường thuộc tính đúng chuẩn nguồn.
/// </summary>
public class File_UploadFileResult
{
    public string? Url { get; set; }              // URL đầy đủ truy cập file (FileController.cs:58)
    public string? FilePath { get; set; }         // Đường dẫn ảo ~/TempFiles/{fileId}{ext} (FileController.cs:59)
    public string? AttFileId { get; set; }        // Mã file kèm đuôi mở rộng {fileId}{ext} (FileController.cs:60)
    public string? FileName { get; set; }         // Tên file gốc người dùng tải lên (FileController.cs:61)
    public string? FileSize { get; set; }         // Dung lượng file dạng chuỗi số byte (FileController.cs:62)
    public string? FlagFileUpload { get; set; }   // Cờ file upload, cố định "1" (FileController.cs:63)
}
