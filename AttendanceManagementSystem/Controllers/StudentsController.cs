using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AttendanceManagementSystem.Data;
using AttendanceManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace AttendanceManagementSystem.Controllers
{
    [Authorize(Policy = "AdminOrTeacher")]
    public class StudentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public StudentsController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Students
        public async Task<IActionResult> Index()
        {
            var students = await _context.Students
                .Include(s => s.Section)
                .ThenInclude(s => s!.Batch)
                .ToListAsync();
            return View(students);
        }

        // GET: Students/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students
                .Include(s => s.Section)
                .ThenInclude(s => s!.Batch)
                .FirstOrDefaultAsync(m => m.Id == id);
            
            if (student == null)
            {
                return NotFound();
            }

            return View(student);
        }

        // GET: Students/Create
        [Authorize(Policy = "AdminOnly")]
        public IActionResult Create()
        {
            ViewData["SectionId"] = new SelectList(_context.Sections.Include(s => s.Batch), "Id", "Name");
            return View();
        }

        // POST: Students/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Create([Bind("Id,Name,RollNumber,Email,PhoneNumber,Address,SectionId")] Student student)
        {
            if (ModelState.IsValid)
            {
                // Create user account for student
                if (!string.IsNullOrEmpty(student.Email))
                {
                    var user = new IdentityUser
                    {
                        UserName = student.Email,
                        Email = student.Email,
                        EmailConfirmed = true
                    };

                    var result = await _userManager.CreateAsync(user, "Student@123"); // Default password
                    if (result.Succeeded)
                    {
                        await _userManager.AddToRoleAsync(user, "Student");
                        student.UserId = user.Id;
                    }
                    else
                    {
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError("", error.Description);
                        }
                        ViewData["SectionId"] = new SelectList(_context.Sections.Include(s => s.Batch), "Id", "Name", student.SectionId);
                        return View(student);
                    }
                }

                _context.Add(student);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Student created successfully! Default password: Student@123";
                return RedirectToAction(nameof(Index));
            }
            ViewData["SectionId"] = new SelectList(_context.Sections.Include(s => s.Batch), "Id", "Name", student.SectionId);
            return View(student);
        }

        // GET: Students/Edit/5
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students.FindAsync(id);
            if (student == null)
            {
                return NotFound();
            }
            ViewData["SectionId"] = new SelectList(_context.Sections.Include(s => s.Batch), "Id", "Name", student.SectionId);
            return View(student);
        }

        // POST: Students/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,RollNumber,Email,PhoneNumber,Address,SectionId,UserId")] Student student)
        {
            if (id != student.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(student);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Student updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!StudentExists(student.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["SectionId"] = new SelectList(_context.Sections.Include(s => s.Batch), "Id", "Name", student.SectionId);
            return View(student);
        }

        // GET: Students/Delete/5
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students
                .Include(s => s.Section)
                .ThenInclude(s => s!.Batch)
                .FirstOrDefaultAsync(m => m.Id == id);
            
            if (student == null)
            {
                return NotFound();
            }

            return View(student);
        }

        // POST: Students/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var student = await _context.Students
                .Include(s => s.Section) // optional, if you need Section info
                .FirstOrDefaultAsync(s => s.Id == id);

            if (student != null)
            {
                // Delete related attendance records first
                var attendances = await _context.Attendances
                    .Where(a => a.StudentId == student.Id)
                    .ToListAsync();

                if (attendances.Any())
                {
                    _context.Attendances.RemoveRange(attendances);
                }

                // Delete associated Identity user
                if (!string.IsNullOrEmpty(student.UserId))
                {
                    var user = await _userManager.FindByIdAsync(student.UserId);
                    if (user != null)
                    {
                        await _userManager.DeleteAsync(user);
                    }
                }

                // Delete the student
                _context.Students.Remove(student);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Student and related attendance deleted successfully!";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool StudentExists(int id)
        {
            return _context.Students.Any(e => e.Id == id);
        }
    }
}
