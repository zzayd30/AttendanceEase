using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using AttendanceManagementSystem.Data;
using AttendanceManagementSystem.Models;
using AttendanceManagementSystem.Controllers.Api;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace AttendanceManagementSystem.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Default cookie authentication for logged-in web users
    public class AttendanceApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AttendanceApiController> _logger;

        public AttendanceApiController(ApplicationDbContext context, ILogger<AttendanceApiController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("students/{courseId}")]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> GetStudentsByCourse(int courseId)
        {
            try
            {
                var students = await _context.Students
                    .Where(s => s.SectionId == courseId)
                    .Select(s => new
                    {
                        s.Id,
                        s.Name,
                        s.Email,
                        s.RollNumber,
                        LastAttendance = _context.Attendances
                            .Where(a => a.StudentId == s.Id && a.Course != null && a.Course.Id == courseId)
                            .OrderByDescending(a => a.Date)
                            .Select(a => new { a.Date, a.Status })
                            .FirstOrDefault()
                    })
                    .ToListAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = students
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting students for course {CourseId}", courseId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while fetching students"
                });
            }
        }

        [HttpPost("mark")]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> MarkAttendance([FromBody] MarkAttendanceRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Invalid request data",
                        Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                    });
                }

                var course = await _context.Courses.FindAsync(request.CourseId);
                if (course == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Course not found"
                    });
                }

                var attendanceRecords = new List<Attendance>();
                var today = DateTime.Today;

                foreach (var studentAttendance in request.StudentAttendances)
                {
                    // Check if attendance already exists for today
                    var existingAttendance = await _context.Attendances
                        .FirstOrDefaultAsync(a => a.StudentId == studentAttendance.StudentId 
                                                && a.CourseId == request.CourseId 
                                                && a.Date.Date == today);

                    if (existingAttendance != null)
                    {
                        // Update existing attendance
                        existingAttendance.Status = studentAttendance.Status;
                        existingAttendance.MarkedAt = DateTime.UtcNow;
                        existingAttendance.Remarks = studentAttendance.Remarks;
                    }
                    else
                    {
                        // Create new attendance record
                        var attendance = new Attendance
                        {
                            StudentId = studentAttendance.StudentId,
                            CourseId = request.CourseId,
                            Date = today,
                            Status = studentAttendance.Status,
                            MarkedAt = DateTime.UtcNow,
                            Remarks = studentAttendance.Remarks
                        };
                        attendanceRecords.Add(attendance);
                    }
                }

                if (attendanceRecords.Any())
                {
                    await _context.Attendances.AddRangeAsync(attendanceRecords);
                }

                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = $"Attendance marked successfully for {request.StudentAttendances.Count} students",
                    Data = new
                    {
                        RecordsUpdated = request.StudentAttendances.Count,
                        Date = today,
                        Course = course.CourseName
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking attendance for course {CourseId}", request.CourseId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while marking attendance"
                });
            }
        }

        [HttpGet("report/{studentId}")]
        public async Task<IActionResult> GetStudentAttendanceReport(int studentId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var student = await _context.Students.FindAsync(studentId);
                if (student == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Student not found"
                    });
                }

                var from = fromDate ?? DateTime.Today.AddDays(-30);
                var to = toDate ?? DateTime.Today;

                var attendanceRecords = await _context.Attendances
                    .Where(a => a.StudentId == studentId && a.Date >= from && a.Date <= to)
                    .Include(a => a.Course)
                    .Select(a => new
                    {
                        a.Id,
                        a.Date,
                        a.Status,
                        Course = a.Course != null ? a.Course.CourseName : "Unknown",
                        a.MarkedAt,
                        a.Remarks
                    })
                    .OrderByDescending(a => a.Date)
                    .ToListAsync();

                var totalDays = attendanceRecords.Count;
                var presentDays = attendanceRecords.Count(a => a.Status == AttendanceStatus.Present);
                var attendancePercentage = totalDays > 0 ? (decimal)presentDays / totalDays * 100 : 0;

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = new
                    {
                        Student = new { student.Name, student.RollNumber },
                        Period = new { From = from, To = to },
                        Summary = new
                        {
                            TotalDays = totalDays,
                            PresentDays = presentDays,
                            AbsentDays = totalDays - presentDays,
                            AttendancePercentage = Math.Round(attendancePercentage, 2)
                        },
                        Records = attendanceRecords
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting attendance report for student {StudentId}", studentId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while fetching attendance report"
                });
            }
        }

        [HttpGet("course-summary/{courseId}")]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> GetCourseAttendanceSummary(int courseId, DateTime? date = null)
        {
            try
            {
                var course = await _context.Courses.FindAsync(courseId);
                if (course == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Course not found"
                    });
                }

                var targetDate = date ?? DateTime.Today;

                var students = await _context.Students
                    .Where(s => s.SectionId == courseId)
                    .Select(s => new
                    {
                        s.Id,
                        s.Name,
                        s.RollNumber,
                        Attendance = _context.Attendances
                            .Where(a => a.StudentId == s.Id && a.CourseId == courseId && a.Date.Date == targetDate.Date)
                            .Select(a => new { a.Status, a.MarkedAt, a.Remarks })
                            .FirstOrDefault()
                    })
                    .ToListAsync();

                var totalStudents = students.Count;
                var markedAttendance = students.Count(s => s.Attendance != null);
                var presentStudents = students.Count(s => s.Attendance?.Status == AttendanceStatus.Present);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = new
                    {
                        Course = new { course.CourseName, course.Id },
                        Date = targetDate.Date,
                        Summary = new
                        {
                            TotalStudents = totalStudents,
                            MarkedAttendance = markedAttendance,
                            PresentStudents = presentStudents,
                            AbsentStudents = markedAttendance - presentStudents,
                            UnmarkedStudents = totalStudents - markedAttendance,
                            AttendancePercentage = markedAttendance > 0 ? Math.Round((decimal)presentStudents / markedAttendance * 100, 2) : 0
                        },
                        Students = students
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting course attendance summary for course {CourseId}", courseId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while fetching course attendance summary"
                });
            }
        }

        [HttpDelete("delete/{attendanceId}")]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> DeleteAttendanceRecord(int attendanceId)
        {
            try
            {
                var attendance = await _context.Attendances.FindAsync(attendanceId);
                if (attendance == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Attendance record not found"
                    });
                }

                _context.Attendances.Remove(attendance);
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Attendance record deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting attendance record {AttendanceId}", attendanceId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while deleting attendance record"
                });
            }
        }
    }

    public class MarkAttendanceRequest
    {
        public int CourseId { get; set; }
        public List<StudentAttendanceDto> StudentAttendances { get; set; } = new();
    }

    public class StudentAttendanceDto
    {
        public int StudentId { get; set; }
        public AttendanceStatus Status { get; set; }
        public string? Remarks { get; set; }
    }}