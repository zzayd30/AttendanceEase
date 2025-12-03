using AttendanceManagementSystem.Data;
using AttendanceManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AttendanceManagementSystem.Controllers
{
    [Authorize(Policy = "AdminOrTeacher")]
    public class TimeTablesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public TimeTablesController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: TimeTables
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var roles = await _userManager.GetRolesAsync(user!);

            IQueryable<TimeTable> timeTablesQuery = _context.TimeTables
                .Include(t => t.Course)
                .Include(t => t.Section)
                .ThenInclude(s => s.Batch)
                .Include(t => t.Teacher);

            if (roles.Contains("Teacher"))
            {
                var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == user!.Id);
                if (teacher != null)
                {
                    timeTablesQuery = timeTablesQuery.Where(t => t.TeacherId == teacher.Id);
                }
            }

            var timeTables = await timeTablesQuery
                .OrderBy(t => t.DayOfWeek)
                .ThenBy(t => t.StartTime)
                .ToListAsync();

            return View(timeTables);
        }

        // GET: TimeTables/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var timeTable = await _context.TimeTables
                .Include(t => t.Course)
                .Include(t => t.Section)
                .ThenInclude(s => s.Batch)
                .Include(t => t.Teacher)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (timeTable == null)
            {
                return NotFound();
            }

            return View(timeTable);
        }

        // GET: TimeTables/Create
        [Authorize(Policy = "AdminOnly")]
        public IActionResult Create()
        {
            ViewData["CourseId"] = new SelectList(_context.Courses, "Id", "CourseName");
            ViewData["SectionId"] = new SelectList(_context.Sections.Include(s => s.Batch), "Id", "Name");
            ViewData["TeacherId"] = new SelectList(_context.Teachers, "Id", "Name");
            return View();
        }

        // POST: TimeTables/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Create([Bind("Id,DayOfWeek,StartTime,EndTime,RoomNumber,CourseId,SectionId,TeacherId")] TimeTable timeTable)
        {
            if (ModelState.IsValid)
            {
                // Check for time conflicts
                var conflict = await _context.TimeTables
                    .AnyAsync(tt => tt.TeacherId == timeTable.TeacherId &&
                                    tt.DayOfWeek == timeTable.DayOfWeek &&
                                    ((timeTable.StartTime >= tt.StartTime && timeTable.StartTime < tt.EndTime) ||
                                     (timeTable.EndTime > tt.StartTime && timeTable.EndTime <= tt.EndTime) ||
                                     (timeTable.StartTime <= tt.StartTime && timeTable.EndTime >= tt.EndTime)));

                if (conflict)
                {
                    ModelState.AddModelError("", "Time conflict detected! Teacher already has a class at this time.");
                }
                else
                {
                    _context.Add(timeTable);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Timetable entry created successfully!";
                    return RedirectToAction(nameof(Index));
                }
            }

            ViewData["CourseId"] = new SelectList(_context.Courses, "Id", "CourseName", timeTable.CourseId);
            ViewData["SectionId"] = new SelectList(_context.Sections.Include(s => s.Batch), "Id", "Name", timeTable.SectionId);
            ViewData["TeacherId"] = new SelectList(_context.Teachers, "Id", "Name", timeTable.TeacherId);
            return View(timeTable);
        }

        // GET: TimeTables/Edit/5
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var timeTable = await _context.TimeTables.FindAsync(id);
            if (timeTable == null)
            {
                return NotFound();
            }

            ViewData["CourseId"] = new SelectList(_context.Courses, "Id", "CourseName", timeTable.CourseId);
            ViewData["SectionId"] = new SelectList(_context.Sections.Include(s => s.Batch), "Id", "Name", timeTable.SectionId);
            ViewData["TeacherId"] = new SelectList(_context.Teachers, "Id", "Name", timeTable.TeacherId);
            return View(timeTable);
        }

        // POST: TimeTables/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,DayOfWeek,StartTime,EndTime,RoomNumber,CourseId,SectionId,TeacherId")] TimeTable timeTable)
        {
            if (id != timeTable.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Check for time conflicts (excluding current entry)
                    var conflict = await _context.TimeTables
                        .AnyAsync(tt => tt.Id != timeTable.Id &&
                                        tt.TeacherId == timeTable.TeacherId &&
                                        tt.DayOfWeek == timeTable.DayOfWeek &&
                                        ((timeTable.StartTime >= tt.StartTime && timeTable.StartTime < tt.EndTime) ||
                                         (timeTable.EndTime > tt.StartTime && timeTable.EndTime <= tt.EndTime) ||
                                         (timeTable.StartTime <= tt.StartTime && timeTable.EndTime >= tt.EndTime)));

                    if (conflict)
                    {
                        ModelState.AddModelError("", "Time conflict detected! Teacher already has a class at this time.");
                    }
                    else
                    {
                        _context.Update(timeTable);
                        await _context.SaveChangesAsync();
                        TempData["Success"] = "Timetable entry updated successfully!";
                        return RedirectToAction(nameof(Index));
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TimeTableExists(timeTable.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            ViewData["CourseId"] = new SelectList(_context.Courses, "Id", "CourseName", timeTable.CourseId);
            ViewData["SectionId"] = new SelectList(_context.Sections.Include(s => s.Batch), "Id", "Name", timeTable.SectionId);
            ViewData["TeacherId"] = new SelectList(_context.Teachers, "Id", "Name", timeTable.TeacherId);
            return View(timeTable);
        }

        // GET: TimeTables/Delete/5
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var timeTable = await _context.TimeTables
                .Include(t => t.Course)
                .Include(t => t.Section)
                .ThenInclude(s => s.Batch)
                .Include(t => t.Teacher)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (timeTable == null)
            {
                return NotFound();
            }

            return View(timeTable);
        }

        // POST: TimeTables/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var timeTable = await _context.TimeTables.FindAsync(id);
            if (timeTable != null)
            {
                _context.TimeTables.Remove(timeTable);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Timetable entry deleted successfully!";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool TimeTableExists(int id)
        {
            return _context.TimeTables.Any(e => e.Id == id);
        }
    }
}
