using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AttendanceManagementSystem.Data;
using Microsoft.EntityFrameworkCore;
using AttendanceManagementSystem.Models;
using System.ComponentModel.DataAnnotations;

namespace AttendanceManagementSystem.Areas.Identity.Pages.Account.Manage
{
    public partial class IndexModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ApplicationDbContext _context;

        public IndexModel(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        public string Username { get; set; } = string.Empty;

        [TempData]
        public string StatusMessage { get; set; } = string.Empty;

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Display(Name = "Full Name")]
            public string? FullName { get; set; }

            [Phone]
            [Display(Name = "Phone number")]
            public string? PhoneNumber { get; set; }

            [Display(Name = "Employee ID")]
            public string? EmployeeId { get; set; }

            [Display(Name = "Roll Number")]
            public string? RollNumber { get; set; }

            [Display(Name = "Department")]
            public string? Department { get; set; }

            [Display(Name = "Address")]
            public string? Address { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            if (Input.PhoneNumber != phoneNumber)
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    StatusMessage = "Unexpected error when trying to set phone number.";
                    return RedirectToPage();
                }
            }

            // Update role-specific data
            var roles = await _userManager.GetRolesAsync(user);
            
            if (roles.Contains("Teacher"))
            {
                var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == user.Id);
                if (teacher != null)
                {
                    teacher.Name = Input.FullName ?? teacher.Name;
                    teacher.EmployeeId = Input.EmployeeId ?? teacher.EmployeeId;
                    teacher.Department = Input.Department ?? teacher.Department;
                    teacher.PhoneNumber = Input.PhoneNumber ?? teacher.PhoneNumber;
                    _context.Update(teacher);
                }
            }
            else if (roles.Contains("Student"))
            {
                var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == user.Id);
                if (student != null)
                {
                    student.Name = Input.FullName ?? student.Name;
                    student.RollNumber = Input.RollNumber ?? student.RollNumber;
                    student.Address = Input.Address ?? student.Address;
                    student.PhoneNumber = Input.PhoneNumber ?? student.PhoneNumber;
                    _context.Update(student);
                }
            }

            await _context.SaveChangesAsync();
            await _signInManager.RefreshSignInAsync(user);
            
            StatusMessage = "Your profile has been updated";
            return RedirectToPage();
        }

        private async Task LoadAsync(IdentityUser user)
        {
            var userName = await _userManager.GetUserNameAsync(user);
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            var roles = await _userManager.GetRolesAsync(user);

            Username = userName ?? string.Empty;

            Input = new InputModel
            {
                PhoneNumber = phoneNumber
            };

            // Load role-specific data
            if (roles.Contains("Teacher"))
            {
                var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == user.Id);
                if (teacher != null)
                {
                    Input.FullName = teacher.Name;
                    Input.EmployeeId = teacher.EmployeeId;
                    Input.Department = teacher.Department;
                }
            }
            else if (roles.Contains("Student"))
            {
                var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == user.Id);
                if (student != null)
                {
                    Input.FullName = student.Name;
                    Input.RollNumber = student.RollNumber;
                    Input.Address = student.Address;
                }
            }
        }
    }
}