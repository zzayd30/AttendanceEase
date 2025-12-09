using AttendanceManagementSystem.Data;
using AttendanceManagementSystem.Models;
using AttendanceManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AttendanceManagementSystem.Controllers
{
    [Authorize]
    public class AttendanceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public AttendanceController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Attendance
        [Authorize(Policy = "AdminOrTeacher")]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var roles = await _userManager.GetRolesAsync(user!);

            IQueryable<Attendance> attendanceQuery = _context.Attendances
                .Include(a => a.Student)
                .Include(a => a.Course)
                .Include(a => a.Section)
                .Include(a => a.Teacher);

            if (roles.Contains("Teacher"))
            {
                var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == user!.Id);
                if (teacher != null)
                {
                    attendanceQuery = attendanceQuery.Where(a => a.TeacherId == teacher.Id);
                }
            }

            var attendances = await attendanceQuery
                .OrderByDescending(a => a.Date)
                .Take(100)
                .ToListAsync();

            return View(attendances);
        }

        // GET: Attendance/Mark
        [Authorize(Policy = "AdminOrTeacher")]
        public async Task<IActionResult> Mark()
        {
            var user = await _userManager.GetUserAsync(User);
            var roles = await _userManager.GetRolesAsync(user!);

            if (roles.Contains("Teacher"))
            {
                var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == user!.Id);
                if (teacher != null)
                {
                    var today = (int)DateTime.Today.DayOfWeek;

                    var todayClasses = await _context.TimeTables
                        .Include(tt => tt.Course)
                        .Include(tt => tt.Section)
                        .Where(tt => tt.TeacherId == teacher.Id && (int)tt.DayOfWeek == today)
                        .ToListAsync();

                    ViewBag.TodayClasses = todayClasses;
                    // Get today's classes for the teacher
                    //var todayClasses = await _context.TimeTables
                    //    .Include(tt => tt.Course)
                    //    .Include(tt => tt.Section)
                    //    .Where(tt => tt.TeacherId == teacher.Id && tt.DayOfWeek == DateTime.Today.DayOfWeek)
                    //    .ToListAsync();

                    //ViewBag.TodayClasses = todayClasses;
                }
            }
            else if (roles.Contains("Admin"))
            {
                ViewBag.Courses = new SelectList(await _context.Courses.ToListAsync(), "Id", "CourseName");
                ViewBag.Sections = new SelectList(await _context.Sections.Include(s => s.Batch).ToListAsync(), "Id", "Name");
            }

            return View();
        }

        // GET: Attendance/MarkByCourse
        [Authorize(Policy = "AdminOrTeacher")]
        public async Task<IActionResult> MarkByCourse(int courseId, int sectionId, DateTime? date)
        {
            var course = await _context.Courses.FindAsync(courseId);
            var section = await _context.Sections.Include(s => s.Batch).FirstOrDefaultAsync(s => s.Id == sectionId);

            if (course == null || section == null)
            {
                TempData["Error"] = "Invalid course or section selected.";
                return RedirectToAction(nameof(Mark));
            }

            var selectedDate = date ?? DateTime.Today;

            // Get students in the section
            var students = await _context.Students
                .Where(s => s.SectionId == sectionId)
                .OrderBy(s => s.RollNumber)
                .ToListAsync();

            // Check if attendance already exists for this date
            var existingAttendance = await _context.Attendances
                .Where(a => a.CourseId == courseId && a.SectionId == sectionId && a.Date.Date == selectedDate.Date)
                .ToListAsync();

            var viewModel = new AttendanceViewModel
            {
                CourseId = courseId,
                CourseName = course.CourseName,
                SectionId = sectionId,
                SectionName = $"{section.Batch?.Name} - {section.Name}",
                Date = selectedDate,
                Students = students.Select(s => new StudentAttendanceItem
                {
                    StudentId = s.Id,
                    StudentName = s.Name,
                    RollNumber = s.RollNumber,
                    Status = existingAttendance.FirstOrDefault(a => a.StudentId == s.Id)?.Status ?? AttendanceStatus.Present,
                    Remarks = existingAttendance.FirstOrDefault(a => a.StudentId == s.Id)?.Remarks
                }).ToList()
            };

            return View(viewModel);
        }

        // POST: Attendance/MarkByCourse
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOrTeacher")]
        public async Task<IActionResult> MarkByCourse(AttendanceViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            var roles = await _userManager.GetRolesAsync(user!);
            
            int? teacherId = null;
            
            // Determine the teacher ID based on user role
            if (roles.Contains("Teacher"))
            {
                var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == user!.Id);
                teacherId = teacher?.Id;
            }
            else if (roles.Contains("Admin"))
            {
                // For Admin, try to find the assigned teacher for this course and section from timetable
                var assignedTeacher = await _context.TimeTables
                    .Where(tt => tt.CourseId == model.CourseId && tt.SectionId == model.SectionId)
                    .Select(tt => tt.Teacher)
                    .FirstOrDefaultAsync();
                
                teacherId = assignedTeacher?.Id;
            }

            // Delete existing attendance for this date
            var existingAttendance = await _context.Attendances
                .Where(a => a.CourseId == model.CourseId && a.SectionId == model.SectionId && a.Date.Date == model.Date.Date)
                .ToListAsync();

            _context.Attendances.RemoveRange(existingAttendance);

            // Add new attendance records
            foreach (var student in model.Students)
            {
                var attendance = new Attendance
                {
                    StudentId = student.StudentId,
                    CourseId = model.CourseId,
                    SectionId = model.SectionId,
                    Date = model.Date,
                    Status = student.Status,
                    Remarks = student.Remarks,
                    MarkedAt = DateTime.Now,
                    TeacherId = teacherId
                };

                _context.Attendances.Add(attendance);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Attendance marked successfully!";
            return RedirectToAction(nameof(Index));
        }

        // GET: Attendance/ViewStudent
        [Authorize] // Allow all authenticated users, then check roles inside
        public async Task<IActionResult> ViewStudent(int? studentId)
        {
            Student student;

            if (User.IsInRole("Admin") && studentId.HasValue)
            {
                // Admin can view any student by ID
                student = await _context.Students
                    .Include(s => s.Section)
                    .ThenInclude(s => s.Batch)
                    .FirstOrDefaultAsync(s => s.Id == studentId.Value);
                
                if (student == null)
                {
                    TempData["Error"] = "Student record not found.";
                    return RedirectToAction("Index", "Students");
                }
            }
            else if (User.IsInRole("Teacher"))
            {
                // Teachers can view students in their classes
                var user = await _userManager.GetUserAsync(User);
                var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == user!.Id);
                
                if (teacher == null)
                {
                    TempData["Error"] = "Teacher record not found.";
                    return RedirectToAction("Index", "Home");
                }
                
                // Check if student is provided and if teacher has access to that student's classes
                if (studentId.HasValue)
                {
                    student = await _context.Students
                        .Include(s => s.Section)
                        .ThenInclude(s => s.Batch)
                        .FirstOrDefaultAsync(s => s.Id == studentId.Value);
                        
                    if (student == null)
                    {
                        TempData["Error"] = "Student record not found.";
                        return RedirectToAction("Index", "Home");
                    }
                    
                    // Verify teacher has classes with this student's section
                    var hasAccess = await _context.TimeTables
                        .AnyAsync(tt => tt.TeacherId == teacher.Id && tt.SectionId == student.SectionId);
                        
                    if (!hasAccess)
                    {
                        TempData["Error"] = "You don't have permission to view this student's attendance.";
                        return RedirectToAction("Index", "Home");
                    }
                }
                else
                {
                    TempData["Error"] = "Student ID is required.";
                    return RedirectToAction("Index", "Home");
                }
            }
            else if (User.IsInRole("Student"))
            {
                // Student can view only their own record
                var user = await _userManager.GetUserAsync(User);
                student = await _context.Students
                    .Include(s => s.Section)
                    .ThenInclude(s => s.Batch)
                    .FirstOrDefaultAsync(s => s.UserId == user!.Id);
                    
                if (student == null)
                {
                    TempData["Error"] = "Student record not found.";
                    return RedirectToAction("Index", "Home");
                }
            }
            else
            {
                TempData["Error"] = "Access denied.";
                return RedirectToAction("Index", "Home");
            }

            var attendanceRecords = await _context.Attendances
                .Include(a => a.Course)
                .Include(a => a.Teacher)
                .Where(a => a.StudentId == student.Id)
                .OrderByDescending(a => a.Date)
                .ToListAsync();

            var attendanceStats = attendanceRecords
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
            ViewBag.AttendanceRecords = attendanceRecords;
            ViewBag.AttendanceStats = attendanceStats;

            return View();
        }


        // GET: Attendance/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var attendance = await _context.Attendances
                .Include(a => a.Student)
                .Include(a => a.Course)
                .Include(a => a.Section)
                .Include(a => a.Teacher)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (attendance == null)
            {
                return NotFound();
            }

            return View(attendance);
        }
    }
}
