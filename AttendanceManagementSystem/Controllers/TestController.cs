using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Authorization;

namespace AttendanceManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class TestController : Controller
    {
        private readonly IEmailSender _emailSender;
        private readonly ILogger<TestController> _logger;

        public TestController(IEmailSender emailSender, ILogger<TestController> logger)
        {
            _emailSender = emailSender;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult TestEmail()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> TestEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ViewBag.Error = "Please provide an email address.";
                return View();
            }

            try
            {
                _logger.LogInformation("Testing email send to: {Email}", email);
                
                var testEmailContent = @"
<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
    <h2 style='color: #007bff;'>Email Test Successful!</h2>
    <p>Hello,</p>
    <p>This is a test email from your AttendanceEase application.</p>
    <p>If you're receiving this email, it means your email service is configured correctly!</p>
    <div style='background-color: #d4edda; border: 1px solid #c3e6cb; padding: 15px; border-radius: 5px; margin: 20px 0;'>
        <strong>? Email Service Status: Working</strong>
    </div>
    <p>Timestamp: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + @"</p>
    <hr style='margin: 20px 0;'>
    <p style='color: #666; font-size: 12px;'>
        This is a test email from AttendanceEase.
    </p>
</div>";

                await _emailSender.SendEmailAsync(email, "AttendanceEase - Email Test", testEmailContent);
                
                ViewBag.Success = $"Test email sent successfully to {email}! Please check your inbox.";
                _logger.LogInformation("Test email sent successfully to: {Email}", email);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Failed to send test email: {ex.Message}";
                _logger.LogError(ex, "Failed to send test email to: {Email}", email);
            }

            return View();
        }
    }
}