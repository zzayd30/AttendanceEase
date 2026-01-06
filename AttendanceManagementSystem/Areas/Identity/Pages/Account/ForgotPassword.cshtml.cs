// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace AttendanceManagementSystem.Areas.Identity.Pages.Account
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<ForgotPasswordModel> _logger;

        public ForgotPasswordModel(UserManager<IdentityUser> userManager, IEmailSender emailSender, ILogger<ForgotPasswordModel> logger)
        {
            _userManager = userManager;
            _emailSender = emailSender;
            _logger = logger;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [EmailAddress]
            public string Email { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                _logger.LogInformation("Password reset requested for email: {Email}", Input.Email);

                var user = await _userManager.FindByEmailAsync(Input.Email);
                if (user == null)
                {
                    _logger.LogWarning("Password reset requested for non-existent email: {Email}", Input.Email);
                    // Don't reveal that the user does not exist
                    return RedirectToPage("./ForgotPasswordConfirmation");
                }

                var isEmailConfirmed = await _userManager.IsEmailConfirmedAsync(user);
                _logger.LogInformation("User found: {UserId}, Email confirmed: {EmailConfirmed}", user.Id, isEmailConfirmed);

                if (!isEmailConfirmed)
                {
                    _logger.LogWarning("Password reset requested for unconfirmed email: {Email}", Input.Email);
                    // Don't reveal that the email is not confirmed
                    return RedirectToPage("./ForgotPasswordConfirmation");
                }

                try
                {
                    // Generate password reset token
                    var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    
                    // Include both code and email in the callback URL
                    var callbackUrl = Url.Page(
                        "/Account/ResetPassword",
                        pageHandler: null,
                        values: new { area = "Identity", code = code, email = Input.Email },
                        protocol: Request.Scheme);

                    _logger.LogInformation("Sending password reset email to: {Email}", Input.Email);
                    _logger.LogInformation("Reset URL: {CallbackUrl}", callbackUrl);

                    var emailBody = $@"
<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
    <h2 style='color: #007bff;'>Password Reset Request</h2>
    <p>Hello,</p>
    <p>You have requested to reset your password for your AttendanceEase account.</p>
    <p>Please click the button below to reset your password:</p>
    <div style='text-align: center; margin: 20px 0;'>
        <a href='{HtmlEncoder.Default.Encode(callbackUrl)}' 
           style='background-color: #007bff; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; display: inline-block;'>
           Reset Password
        </a>
    </div>
    <p>If the button doesn't work, you can copy and paste the following link into your browser:</p>
    <p><a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>{HtmlEncoder.Default.Encode(callbackUrl)}</a></p>
    <p><strong>Note:</strong> This link will expire in 24 hours for security reasons.</p>
    <p>If you didn't request this password reset, please ignore this email.</p>
    <hr style='margin: 20px 0;'>
    <p style='color: #666; font-size: 12px;'>
        This email was sent from AttendanceEase. Please do not reply to this email.
    </p>
</div>";

                    await _emailSender.SendEmailAsync(
                        Input.Email,
                        "Reset Your AttendanceEase Password",
                        emailBody);

                    _logger.LogInformation("Password reset email sent successfully to: {Email}", Input.Email);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send password reset email to: {Email}", Input.Email);
                    
                    // Add a user-friendly error message
                    ModelState.AddModelError(string.Empty, "An error occurred while sending the reset email. Please try again later.");
                    return Page();
                }

                return RedirectToPage("./ForgotPasswordConfirmation");
            }

            return Page();
        }
    }
}
