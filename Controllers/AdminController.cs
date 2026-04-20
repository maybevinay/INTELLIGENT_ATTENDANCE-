using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IntelligentAttendanceSystem.Data;
using IntelligentAttendanceSystem.Models;
using IntelligentAttendanceSystem.Services;
using System.Security.Claims;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;

namespace IntelligentAttendanceSystem.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly FaceService _faceService;

        public AdminController(ApplicationDbContext context, FaceService faceService)
        {
            _context = context;
            _faceService = faceService;
        }

        // ─── Helper: check if current user is an Admin ───────────────────────
        private bool IsAdmin()
        {
            if (!User.Identity?.IsAuthenticated ?? true) return false;
            return User.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "Admin");
        }

        // ─── Page actions (redirect unauthenticated users to login) ──────────

        [Authorize]
        public IActionResult DashboardOverview()
        {
            if (!IsAdmin()) return RedirectToAction("AccessDenied", "Account");
            return View();
        }

        [Authorize]
        public IActionResult Dashboard()
        {
            if (!IsAdmin()) return RedirectToAction("AccessDenied", "Account");
            return View();
        }

        [Authorize]
        public IActionResult ManageUsers()
        {
            if (!IsAdmin()) return RedirectToAction("AccessDenied", "Account");
            var users = _context.Users.ToList();
            return View(users);
        }

        [Authorize]
        [HttpGet]
        public IActionResult AddUser()
        {
            if (!IsAdmin()) return RedirectToAction("AccessDenied", "Account");
            return View();
        }

        [Authorize]
        public IActionResult Analytics()
        {
            if (!IsAdmin()) return RedirectToAction("AccessDenied", "Account");
            return View();
        }

        [Authorize]
        public IActionResult Reports()
        {
            if (!IsAdmin()) return RedirectToAction("AccessDenied", "Account");
            return View();
        }

        [Authorize]
        public async Task<IActionResult> Records()
        {
            if (!IsAdmin()) return RedirectToAction("AccessDenied", "Account");
            var records = await _context.AttendanceRecords
                .Include(a => a.User)
                .OrderByDescending(a => a.Timestamp)
                .ToListAsync();
            return View(records);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ExportRecords()
        {
            if (!IsAdmin()) return RedirectToAction("AccessDenied", "Account");

            var records = await _context.AttendanceRecords
                .Include(a => a.User)
                .OrderByDescending(a => a.Timestamp)
                .ToListAsync();

            var csv = new StringBuilder();
            csv.AppendLine("Record ID,User Name,Email,Timestamp,Status");

            foreach (var record in records)
            {
                // Ensure values are CSV safe (basic escaping)
                string name = record.User?.Name?.Replace(",", " ") ?? "Unknown";
                string email = record.User?.Email?.Replace(",", " ") ?? "Unknown";
                string timestamp = record.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
                string status = record.Status ?? "N/A";

                csv.AppendLine($"{record.Id},{name},{email},{timestamp},{status}");
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"Attendance_Records_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> EditUser(int id)
        {
            if (!IsAdmin()) return RedirectToAction("AccessDenied", "Account");
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();
            return View(user);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> EditUser(User user)
        {
            if (!IsAdmin()) return RedirectToAction("AccessDenied", "Account");
            if (!ModelState.IsValid) return View(user);

            var existingUser = await _context.Users.FindAsync(user.Id);
            if (existingUser == null) return NotFound();

            existingUser.Name = user.Name;
            existingUser.Email = user.Email;
            existingUser.Role = user.Role;
            existingUser.Department = user.Department;

            _context.Users.Update(existingUser);
            await _context.SaveChangesAsync();

            return RedirectToAction("ManageUsers");
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> DeleteUser(int id)
        {
            if (!IsAdmin()) return RedirectToAction("AccessDenied", "Account");
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return RedirectToAction("ManageUsers");
        }

        [Authorize]
        public IActionResult Alerts()
        {
            if (!IsAdmin()) return RedirectToAction("AccessDenied", "Account");
            return View();
        }

        // ─── API actions (return JSON errors, never redirect) ─────────────────

        [HttpPost]
        public async Task<IActionResult> AddUser([FromBody] AddUserRequest request)
        {
            // Return JSON 401 — NOT an HTML redirect — so fetch() can handle it
            if (!User.Identity?.IsAuthenticated ?? true)
                return Unauthorized(new { success = false, message = "You must be logged in as Admin." });
            if (!IsAdmin())
                return StatusCode(403, new { success = false, message = "Admin access required." });

            Console.WriteLine("AddUser API hit...");

            if (request == null)
            {
                Console.WriteLine("Request body is null!");
                return BadRequest(new { success = false, message = "Payload is null." });
            }

            Console.WriteLine($"Registering: {request.Name}, {request.Email}");

            if (string.IsNullOrEmpty(request.Name) || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.ImageBase64))
            {
                Console.WriteLine("Validation failed: Name, Email or Image missing.");
                return BadRequest(new { success = false, message = "Missing required fields or biometric data." });
            }

            try
            {
                if (_context.Users.Any(u => u.Email == request.Email))
                {
                    Console.WriteLine("User exists!");
                    return BadRequest(new { success = false, message = "User with this email already exists." });
                }

                var newUser = new User
                {
                    Name = request.Name,
                    Email = request.Email,
                    Role = request.Role ?? "Student",
                    Department = request.Department ?? "General",
                    FaceIdentifier = Guid.NewGuid().ToString()
                };

                // --- Real Face Registration ---
                var faceResult = await _faceService.RegisterFaceAsync(request.ImageBase64, newUser.FaceIdentifier);
                if (!faceResult.Success)
                {
                    return BadRequest(new { success = false, message = "Face Registration Failed: " + faceResult.Message });
                }

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                Console.WriteLine("User saved successfully!");
                return Ok(new { success = true, message = "User successfully registered with biometric encoding." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR saving user: {ex.Message}");
                if (ex.InnerException != null) Console.WriteLine($"Inner: {ex.InnerException.Message}");
                return StatusCode(500, new { success = false, message = "Database error: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> MarkAttendance([FromBody] AttendanceRequest request)
        {
            try
            {
                // Return JSON 401/403 — NOT an HTML redirect
                if (!User.Identity?.IsAuthenticated ?? true)
                    return Unauthorized(new { success = false, message = "You must be logged in as Admin." });
                if (!IsAdmin())
                    return StatusCode(403, new { success = false, message = "Admin access required." });

                if (string.IsNullOrEmpty(request?.ImageBase64))
                {
                    return BadRequest(new { success = false, message = "No image provided" });
                }

                var students = await _context.Users.Where(u => u.Role == "Student").ToListAsync();
                if (!students.Any())
                {
                    return Json(new { success = false, message = "No students exist in database. Register students first." });
                }

                // --- Real Face Verification ---
                var faceResult = await _faceService.VerifyFaceAsync(request.ImageBase64);

                if (!faceResult.Success)
                {
                    return Json(new { success = false, message = "Verification Error: " + faceResult.Message });
                }

                if (!faceResult.Match)
                {
                    return Json(new { success = false, message = "Face not recognized. Access Denied." });
                }

                var recognizedStudent = await _context.Users.FirstOrDefaultAsync(u => u.FaceIdentifier == faceResult.Identifier);
                if (recognizedStudent == null)
                {
                    return Json(new { success = false, message = "Match found but student record missing in DB." });
                }

                var record = new AttendanceRecord
                {
                    UserId = recognizedStudent.Id,
                    Timestamp = DateTime.Now,
                    Status = "Present",
                    CapturedImagePath = "" 
                };

                _context.AttendanceRecords.Add(record);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"✓ Attendance marked for {recognizedStudent.Name} ({recognizedStudent.Email}) at {record.Timestamp:HH:mm:ss} (Confidence: {faceResult.Confidence:P0})"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Backend Error: " + ex.Message });
            }
        }

    }

    public class AttendanceRequest
    {
        public string ImageBase64 { get; set; } = "";
    }

    public class AddUserRequest
    {
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string Role { get; set; } = "";
        public string Department { get; set; } = "";
        public string ImageBase64 { get; set; } = "";
    }
}
