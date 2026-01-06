using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;

namespace AttendanceManagementSystem.Services
{
    public class EmailSettings
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = string.Empty;
        public bool EnableSSL { get; set; } = true;
        public bool SaveToFile { get; set; } = false;
        public string EmailSaveDirectory { get; set; } = "wwwroot/emails";
        public bool DevelopmentMode { get; set; } = false;
    }

    public class EmailService : IEmailSender
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;
        private readonly IWebHostEnvironment _environment;

        public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger, IWebHostEnvironment environment)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
            _environment = environment;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_emailSettings.FromName, _emailSettings.FromEmail));
                message.To.Add(new MailboxAddress("", email));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = htmlMessage
                };
                message.Body = bodyBuilder.ToMessageBody();

                // Check if we should use development mode
                bool isDevelopmentMode = _emailSettings.DevelopmentMode || 
                                       _emailSettings.Host == "localhost" || 
                                       string.IsNullOrEmpty(_emailSettings.Host) ||
                                       string.IsNullOrEmpty(_emailSettings.Username);

                if (isDevelopmentMode)
                {
                    await HandleDevelopmentEmail(email, subject, htmlMessage, message);
                    return;
                }

                // Send real email
                await SendRealEmailAsync(message);
                _logger.LogInformation("Email sent successfully to {Email} via {Host}", email, _emailSettings.Host);
                
                Console.WriteLine($"? Email sent successfully to {email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}", email);
                
                // Log detailed error information
                Console.WriteLine($"? Failed to send email to {email}");
                Console.WriteLine($"Error: {ex.Message}");
                
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                
                throw new InvalidOperationException($"Failed to send email: {ex.Message}", ex);
            }
        }

        private async Task HandleDevelopmentEmail(string email, string subject, string htmlMessage, MimeMessage message)
        {
            _logger.LogInformation("=== EMAIL SERVICE (DEVELOPMENT MODE) ===");
            _logger.LogInformation("To: {Email}", email);
            _logger.LogInformation("Subject: {Subject}", subject);
            _logger.LogInformation("Body: {Body}", htmlMessage);
            _logger.LogInformation("==========================================");

            // Save to file if configured
            if (_emailSettings.SaveToFile)
            {
                await SaveEmailToFileAsync(email, subject, htmlMessage);
            }

            // Also display in a more visible way
            Console.WriteLine("\n" + new string('=', 60));
            Console.WriteLine("?? EMAIL SENT (Development Mode)");
            Console.WriteLine(new string('=', 60));
            Console.WriteLine($"To: {email}");
            Console.WriteLine($"Subject: {subject}");
            Console.WriteLine($"Content: {htmlMessage}");
            Console.WriteLine(new string('=', 60) + "\n");
        }

        private async Task SaveEmailToFileAsync(string email, string subject, string htmlMessage)
        {
            try
            {
                var emailDir = Path.Combine(_environment.ContentRootPath, _emailSettings.EmailSaveDirectory);
                
                if (!Directory.Exists(emailDir))
                {
                    Directory.CreateDirectory(emailDir);
                }

                var fileName = $"email_{DateTime.Now:yyyyMMdd_HHmmss}_{email.Replace("@", "_at_")}.html";
                var filePath = Path.Combine(emailDir, fileName);

                var emailContent = $@"
<!DOCTYPE html>
<html>
<head>
    <title>{subject}</title>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; }}
        .header {{ background: #007bff; color: white; padding: 15px; border-radius: 5px; }}
        .content {{ background: #f8f9fa; padding: 20px; border-radius: 5px; margin-top: 10px; }}
        .footer {{ margin-top: 20px; color: #6c757d; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='header'>
        <h2>?? Email from AttendanceEase</h2>
        <p><strong>To:</strong> {email}</p>
        <p><strong>Subject:</strong> {subject}</p>
        <p><strong>Sent:</strong> {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>
    </div>
    <div class='content'>
        {htmlMessage}
    </div>
    <div class='footer'>
        <p>This email was saved during development testing.</p>
    </div>
</body>
</html>";

                await File.WriteAllTextAsync(filePath, emailContent);
                _logger.LogInformation("Email saved to file: {FilePath}", filePath);
                
                Console.WriteLine($"?? Email saved to: {filePath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save email to file");
            }
        }

        private async Task SendRealEmailAsync(MimeMessage message)
        {
            using var client = new SmtpClient();

            Console.WriteLine($"?? Connecting to SMTP server: {_emailSettings.Host}:{_emailSettings.Port}");
            
            // Connect to the SMTP server
            await client.ConnectAsync(_emailSettings.Host, _emailSettings.Port, 
                _emailSettings.EnableSSL ? SecureSocketOptions.StartTls : SecureSocketOptions.None);

            Console.WriteLine("? Connected to SMTP server");

            // Authenticate if credentials are provided
            if (!string.IsNullOrEmpty(_emailSettings.Username))
            {
                Console.WriteLine($"?? Authenticating with username: {_emailSettings.Username}");
                await client.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password);
                Console.WriteLine("? Authentication successful");
            }

            // Send the email
            Console.WriteLine("?? Sending email...");
            await client.SendAsync(message);
            Console.WriteLine("? Email sent successfully");

            // Disconnect
            await client.DisconnectAsync(true);
            Console.WriteLine("?? Disconnected from SMTP server");
        }
    }
}