using AttendanceManagementSystem.Data;
using AttendanceManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace AttendanceManagementSystem.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ReportsController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Reports
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var roles = await _userManager.GetRolesAsync(user!);

            ViewBag.Courses = new SelectList(await _context.Courses.ToListAsync(), "Id", "CourseName");
            ViewBag.Sections = new SelectList(await _context.Sections.Include(s => s.Batch).ToListAsync(), "Id", "Name");
            ViewBag.Batches = new SelectList(await _context.Batches.ToListAsync(), "Id", "Name");

            return View();
        }

        // GET: Reports/CourseAttendance
        public async Task<IActionResult> CourseAttendance(int? courseId, int? sectionId, DateTime? startDate, DateTime? endDate)
        {
            if (courseId == null || sectionId == null)
            {
                TempData["Error"] = "Please select both course and section.";
                return RedirectToAction(nameof(Index));
            }

            var course = await _context.Courses.FindAsync(courseId);
            var section = await _context.Sections.Include(s => s.Batch).FirstOrDefaultAsync(s => s.Id == sectionId);

            if (course == null || section == null)
            {
                return NotFound();
            }

            var start = startDate ?? DateTime.Today.AddMonths(-1);
            var end = endDate ?? DateTime.Today;

            var attendanceRecords = await _context.Attendances
                .Include(a => a.Student)
                .Where(a => a.CourseId == courseId && 
                           a.SectionId == sectionId && 
                           a.Date >= start && 
                           a.Date <= end)
                .OrderBy(a => a.Student!.RollNumber)
                .ThenBy(a => a.Date)
                .ToListAsync();

            var students = await _context.Students
                .Where(s => s.SectionId == sectionId)
                .OrderBy(s => s.RollNumber)
                .ToListAsync();

            var reportData = students.Select(student =>
            {
                var studentAttendance = attendanceRecords.Where(a => a.StudentId == student.Id).ToList();
                var totalClasses = studentAttendance.Count;
                var presentCount = studentAttendance.Count(a => a.Status == AttendanceStatus.Present);
                var absentCount = studentAttendance.Count(a => a.Status == AttendanceStatus.Absent);
                var lateCount = studentAttendance.Count(a => a.Status == AttendanceStatus.Late);
                var percentage = totalClasses > 0 ? (double)presentCount / totalClasses * 100 : 0;

                return new
                {
                    Student = student,
                    TotalClasses = totalClasses,
                    PresentCount = presentCount,
                    AbsentCount = absentCount,
                    LateCount = lateCount,
                    Percentage = percentage
                };
            }).ToList();

            ViewBag.Course = course;
            ViewBag.Section = section;
            ViewBag.StartDate = start;
            ViewBag.EndDate = end;
            ViewBag.ReportData = reportData;

            return View();
        }

        // GET: Reports/StudentAttendance
        public async Task<IActionResult> StudentAttendance(int? studentId, DateTime? startDate, DateTime? endDate)
        {
            var user = await _userManager.GetUserAsync(User);
            var roles = await _userManager.GetRolesAsync(user!);

            Student? student = null;

            if (roles.Contains("Student"))
            {
                student = await _context.Students
                    .Include(s => s.Section)
                    .ThenInclude(s => s!.Batch)
                    .FirstOrDefaultAsync(s => s.UserId == user!.Id);
            }
            else if (studentId.HasValue)
            {
                student = await _context.Students
                    .Include(s => s.Section)
                    .ThenInclude(s => s!.Batch)
                    .FirstOrDefaultAsync(s => s.Id == studentId);
            }

            if (student == null)
            {
                if (roles.Contains("Student"))
                {
                    TempData["Error"] = "Student record not found.";
                    return RedirectToAction("Index", "Home");
                }
                
                ViewBag.Students = new SelectList(await _context.Students.OrderBy(s => s.RollNumber).ToListAsync(), "Id", "Name");
                return View("SelectStudent");
            }

            var start = startDate ?? DateTime.Today.AddMonths(-1);
            var end = endDate ?? DateTime.Today;

            var attendanceRecords = await _context.Attendances
                .Include(a => a.Course)
                .Include(a => a.Teacher)
                .Where(a => a.StudentId == student.Id && a.Date >= start && a.Date <= end)
                .OrderByDescending(a => a.Date)
                .ToListAsync();

            var courseStats = attendanceRecords
                .GroupBy(a => a.CourseId)
                .Select(g => new
                {
                    CourseId = g.Key,
                    CourseName = g.First().Course?.CourseName,
                    TotalClasses = g.Count(),
                    PresentCount = g.Count(a => a.Status == AttendanceStatus.Present),
                    AbsentCount = g.Count(a => a.Status == AttendanceStatus.Absent),
                    LateCount = g.Count(a => a.Status == AttendanceStatus.Late),
                    Percentage = g.Count() > 0 ? (double)g.Count(a => a.Status == AttendanceStatus.Present) / g.Count() * 100 : 0
                })
                .ToList();

            ViewBag.Student = student;
            ViewBag.StartDate = start;
            ViewBag.EndDate = end;
            ViewBag.AttendanceRecords = attendanceRecords;
            ViewBag.CourseStats = courseStats;

            return View();
        }

        // GET: Reports/GeneratePDF
        public async Task<IActionResult> GeneratePDF(int? studentId, int? courseId, int? sectionId, DateTime? startDate, DateTime? endDate)
        {
            var user = await _userManager.GetUserAsync(User);
            var roles = await _userManager.GetRolesAsync(user!);

            StringBuilder htmlContent = new StringBuilder();
            htmlContent.Append("<html><head>");
            htmlContent.Append("<style>");
            htmlContent.Append("body { font-family: Arial, sans-serif; margin: 20px; }");
            htmlContent.Append("h1 { color: #0f172a; border-bottom: 3px solid #3b82f6; padding-bottom: 10px; }");
            htmlContent.Append("table { width: 100%; border-collapse: collapse; margin-top: 20px; }");
            htmlContent.Append("th, td { border: 1px solid #ddd; padding: 12px; text-align: left; }");
            htmlContent.Append("th { background-color: #3b82f6; color: white; }");
            htmlContent.Append("tr:nth-child(even) { background-color: #f8fafc; }");
            htmlContent.Append(".header { text-align: center; margin-bottom: 30px; }");
            htmlContent.Append(".info { margin: 20px 0; }");
            htmlContent.Append("</style>");
            htmlContent.Append("</head><body>");

            htmlContent.Append("<div class='header'>");
            htmlContent.Append("<h1>AttendanceEase</h1>");
            htmlContent.Append("<h2>Attendance Report</h2>");
            htmlContent.Append("</div>");

            var start = startDate ?? DateTime.Today.AddMonths(-1);
            var end = endDate ?? DateTime.Today;

            htmlContent.Append("<div class='info'>");
            htmlContent.Append($"<p><strong>Report Generated:</strong> {DateTime.Now:yyyy-MM-dd HH:mm}</p>");
            htmlContent.Append($"<p><strong>Period:</strong> {start:yyyy-MM-dd} to {end:yyyy-MM-dd}</p>");
            htmlContent.Append("</div>");

            if (studentId.HasValue || roles.Contains("Student"))
            {
                Student? student = null;
                if (roles.Contains("Student"))
                {
                    student = await _context.Students
                        .Include(s => s.Section)
                        .ThenInclude(s => s!.Batch)
                        .FirstOrDefaultAsync(s => s.UserId == user!.Id);
                }
                else
                {
                    student = await _context.Students
                        .Include(s => s.Section)
                        .ThenInclude(s => s!.Batch)
                        .FirstOrDefaultAsync(s => s.Id == studentId);
                }

                if (student != null)
                {
                    var attendanceRecords = await _context.Attendances
                        .Include(a => a.Course)
                        .Where(a => a.StudentId == student.Id && a.Date >= start && a.Date <= end)
                        .OrderByDescending(a => a.Date)
                        .ToListAsync();

                    htmlContent.Append("<div class='info'>");
                    htmlContent.Append($"<p><strong>Student Name:</strong> {student.Name}</p>");
                    htmlContent.Append($"<p><strong>Roll Number:</strong> {student.RollNumber}</p>");
                    htmlContent.Append($"<p><strong>Section:</strong> {student.Section?.Batch?.Name} - {student.Section?.Name}</p>");
                    htmlContent.Append("</div>");

                    var courseStats = attendanceRecords
                        .GroupBy(a => a.CourseId)
                        .Select(g => new
                        {
                            CourseName = g.First().Course?.CourseName,
                            TotalClasses = g.Count(),
                            PresentCount = g.Count(a => a.Status == AttendanceStatus.Present),
                            Percentage = g.Count() > 0 ? (double)g.Count(a => a.Status == AttendanceStatus.Present) / g.Count() * 100 : 0
                        })
                        .ToList();

                    htmlContent.Append("<table>");
                    htmlContent.Append("<tr><th>Course</th><th>Total Classes</th><th>Present</th><th>Percentage</th></tr>");
                    foreach (var stat in courseStats)
                    {
                        htmlContent.Append($"<tr><td>{stat.CourseName}</td><td>{stat.TotalClasses}</td><td>{stat.PresentCount}</td><td>{stat.Percentage:F2}%</td></tr>");
                    }
                    htmlContent.Append("</table>");
                }
            }
            else if (courseId.HasValue && sectionId.HasValue)
            {
                var course = await _context.Courses.FindAsync(courseId);
                var section = await _context.Sections.Include(s => s.Batch).FirstOrDefaultAsync(s => s.Id == sectionId);

                if (course != null && section != null)
                {
                    htmlContent.Append("<div class='info'>");
                    htmlContent.Append($"<p><strong>Course:</strong> {course.CourseName}</p>");
                    htmlContent.Append($"<p><strong>Section:</strong> {section.Batch?.Name} - {section.Name}</p>");
                    htmlContent.Append("</div>");

                    var attendanceRecords = await _context.Attendances
                        .Include(a => a.Student)
                        .Where(a => a.CourseId == courseId && a.SectionId == sectionId && a.Date >= start && a.Date <= end)
                        .ToListAsync();

                    var students = await _context.Students
                        .Where(s => s.SectionId == sectionId)
                        .OrderBy(s => s.RollNumber)
                        .ToListAsync();

                    htmlContent.Append("<table>");
                    htmlContent.Append("<tr><th>Roll No</th><th>Name</th><th>Total Classes</th><th>Present</th><th>Absent</th><th>Percentage</th></tr>");

                    foreach (var student in students)
                    {
                        var studentAttendance = attendanceRecords.Where(a => a.StudentId == student.Id).ToList();
                        var totalClasses = studentAttendance.Count;
                        var presentCount = studentAttendance.Count(a => a.Status == AttendanceStatus.Present);
                        var absentCount = studentAttendance.Count(a => a.Status == AttendanceStatus.Absent);
                        var percentage = totalClasses > 0 ? (double)presentCount / totalClasses * 100 : 0;

                        htmlContent.Append($"<tr><td>{student.RollNumber}</td><td>{student.Name}</td><td>{totalClasses}</td><td>{presentCount}</td><td>{absentCount}</td><td>{percentage:F2}%</td></tr>");
                    }
                    htmlContent.Append("</table>");
                }
            }

            htmlContent.Append("</body></html>");

            // Return HTML as downloadable file
            var bytes = Encoding.UTF8.GetBytes(htmlContent.ToString());
            return File(bytes, "text/html", $"AttendanceReport_{DateTime.Now:yyyyMMdd_HHmmss}.html");
        }
    }
}
