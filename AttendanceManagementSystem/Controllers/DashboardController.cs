using AttendanceManagementSystem.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AttendanceManagementSystem.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public DashboardController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var roles = await _userManager.GetRolesAsync(user!);

            if (roles.Contains("Admin"))
            {
                return await AdminDashboard();
            }
            else if (roles.Contains("Teacher"))
            {
                return await TeacherDashboard();
            }
            else if (roles.Contains("Student"))
            {
                return await StudentDashboard();
            }

            return View();
        }

        private async Task<IActionResult> AdminDashboard()
        {
            ViewBag.TotalStudents = await _context.Students.CountAsync();
            ViewBag.TotalTeachers = await _context.Teachers.CountAsync();
            ViewBag.TotalCourses = await _context.Courses.CountAsync();
            ViewBag.TotalBatches = await _context.Batches.CountAsync();
            ViewBag.TotalSections = await _context.Sections.CountAsync();
            ViewBag.TodayAttendance = await _context.Attendances
                .Where(a => a.Date.Date == DateTime.Today)
                .CountAsync();

            return View("AdminDashboard");
        }

        private async Task<IActionResult> TeacherDashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            var teacher = await _context.Teachers
                .FirstOrDefaultAsync(t => t.UserId == user!.Id);

            if (teacher == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var todayClasses = await _context.TimeTables
                .Include(tt => tt.Course)
                .Include(tt => tt.Section)
                .Where(tt => tt.TeacherId == teacher.Id && tt.DayOfWeek == DateTime.Today.DayOfWeek)
                .OrderBy(tt => tt.StartTime)
                .ToListAsync();

            ViewBag.TodayClasses = todayClasses;
            ViewBag.TeacherName = teacher.Name;

            return View("TeacherDashboard");
        }

        private async Task<IActionResult> StudentDashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            var student = await _context.Students
                .Include(s => s.Section)
                .ThenInclude(s => s!.Batch)
                .FirstOrDefaultAsync(s => s.UserId == user!.Id);

            if (student == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var attendanceRecords = await _context.Attendances
                .Include(a => a.Course)
                .Where(a => a.StudentId == student.Id)
                .OrderByDescending(a => a.Date)
                .Take(10)
                .ToListAsync();

            var attendanceStats = await _context.Attendances
                .Where(a => a.StudentId == student.Id)
                .GroupBy(a => a.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            ViewBag.Student = student;
            ViewBag.AttendanceRecords = attendanceRecords;
            ViewBag.AttendanceStats = attendanceStats;
            ViewBag.TotalClasses = attendanceRecords.Count;
            ViewBag.PresentCount = attendanceStats.FirstOrDefault(s => s.Status == Models.AttendanceStatus.Present)?.Count ?? 0;

            return View("StudentDashboard");
        }
    }
}
