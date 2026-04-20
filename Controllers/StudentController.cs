using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IntelligentAttendanceSystem.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace IntelligentAttendanceSystem.Controllers
{
    [Authorize]
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StudentController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Dashboard()
        {
            // Verify student role
            var role = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            if (role != "Student") return RedirectToAction("AccessDenied", "Account");

            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            // Get attendance records
            var records = await _context.AttendanceRecords
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.Timestamp)
                .ToListAsync();

            // Prepare analytics data
            var last30Days = DateTime.Now.AddDays(-30);
            var presentCount = records.Count(r => r.Status == "Present" && r.Timestamp >= last30Days);
            var totalDays = 30; // Assuming 30 working days for demo
            var absentCount = totalDays - presentCount;

            ViewBag.PresentCount = presentCount;
            ViewBag.AbsentCount = absentCount;
            ViewBag.AttendanceRate = totalDays > 0 ? (presentCount * 100 / totalDays) : 0;

            return View(records);
        }
    }
}
