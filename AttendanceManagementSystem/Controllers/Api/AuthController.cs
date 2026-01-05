using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using AttendanceManagementSystem.Services;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace AttendanceManagementSystem.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IJwtService _jwtService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            IJwtService jwtService,
            ILogger<AuthController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtService = jwtService;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginApiRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<object> 
                    { 
                        Success = false, 
                        Message = "Invalid input data",
                        Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                    });
                }

                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null)
                {
                    return BadRequest(new ApiResponse<object> 
                    { 
                        Success = false, 
                        Message = "Invalid email or password" 
                    });
                }

                var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
                if (!result.Succeeded)
                {
                    return BadRequest(new ApiResponse<object> 
                    { 
                        Success = false, 
                        Message = "Invalid email or password" 
                    });
                }

                var token = await _jwtService.GenerateTokenAsync(user);
                var roles = await _userManager.GetRolesAsync(user);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Login successful",
                    Data = new
                    {
                        Token = token.AccessToken,
                        RefreshToken = token.RefreshToken,
                        ExpiresAt = token.ExpiresAt,
                        User = new
                        {
                            user.Id,
                            user.Email,
                            user.UserName,
                            Roles = roles
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for {Email}", request.Email);
                return StatusCode(500, new ApiResponse<object> 
                { 
                    Success = false, 
                    Message = "An error occurred during login" 
                });
            }
        }

        [HttpPost("validate-email")]
        public async Task<IActionResult> ValidateEmail([FromBody] ValidateEmailApiRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Email))
                {
                    return BadRequest(new ApiResponse<object> 
                    { 
                        Success = false, 
                        Message = "Email is required" 
                    });
                }

                if (!new EmailAddressAttribute().IsValid(request.Email))
                {
                    return BadRequest(new ApiResponse<object> 
                    { 
                        Success = false, 
                        Message = "Invalid email format" 
                    });
                }

                var user = await _userManager.FindByEmailAsync(request.Email);
                var exists = user != null;

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = new { Exists = exists }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating email {Email}", request.Email);
                return StatusCode(500, new ApiResponse<object> 
                { 
                    Success = false, 
                    Message = "An error occurred during validation" 
                });
            }
        }

        [HttpPost("check-password-strength")]
        public IActionResult CheckPasswordStrength([FromBody] PasswordStrengthApiRequest request)
        {
            try
            {
                var score = CalculatePasswordStrength(request.Password);
                var requirements = new
                {
                    MinLength = request.Password.Length >= 6,
                    HasUpper = request.Password.Any(char.IsUpper),
                    HasLower = request.Password.Any(char.IsLower),
                    HasDigit = request.Password.Any(char.IsDigit),
                    HasSpecial = request.Password.Any(c => !char.IsLetterOrDigit(c))
                };

                string strengthText = score switch
                {
                    >= 80 => "Strong",
                    >= 60 => "Good",
                    >= 40 => "Fair",
                    >= 20 => "Weak",
                    _ => "Very Weak"
                };

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = new
                    {
                        Score = score,
                        Strength = strengthText,
                        Requirements = requirements,
                        IsValid = requirements.MinLength && requirements.HasUpper && 
                                 requirements.HasLower && requirements.HasDigit
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking password strength");
                return StatusCode(500, new ApiResponse<object> 
                { 
                    Success = false, 
                    Message = "An error occurred during validation" 
                });
            }
        }

        private static int CalculatePasswordStrength(string password)
        {
            var score = 0;
            
            if (password.Length >= 6) score += 20;
            if (password.Length >= 8) score += 10;
            if (password.Length >= 12) score += 10;
            
            if (password.Any(char.IsUpper)) score += 15;
            if (password.Any(char.IsLower)) score += 15;
            if (password.Any(char.IsDigit)) score += 15;
            if (password.Any(c => !char.IsLetterOrDigit(c))) score += 15;
            
            return score;
        }
    }

    public class LoginApiRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class ValidateEmailApiRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    public class PasswordStrengthApiRequest
    {
        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}