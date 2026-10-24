using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using AuthService.Dtos;
using AuthService.Services;
using System.Security.Claims;
using Npgsql;

namespace AuthService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IValidator<CreateUserDto> _createUserValidator;
        private readonly IValidator<RefreshTokenRequestDto> _refreshTokenValidator;
        private readonly IValidator<LoginUserDto> _loginValidator;
        private readonly ILogger<UserController> _logger;
        private readonly IValidator<ChangePasswordDto> _changePasswordValidator;
        private readonly IValidator<DeleteAccountDto> _deleteAccountValidator;
        private readonly IValidator<UpdateAccountDto> _updateAccountValidator;

        public UserController(IAuthService authService, IValidator<CreateUserDto> createUserValidator, IValidator<RefreshTokenRequestDto> refreshTokenValidator, IValidator<LoginUserDto> loginValidator, ILogger<UserController> logger, IValidator<ChangePasswordDto> changePasswordValidator, IValidator<DeleteAccountDto> deleteAccountValidator, IValidator<UpdateAccountDto> updateAccountValidator)
        {
            _authService = authService;
            _createUserValidator = createUserValidator;
            _refreshTokenValidator = refreshTokenValidator;
            _loginValidator = loginValidator;
            _logger = logger;
            _changePasswordValidator = changePasswordValidator;
            _deleteAccountValidator = deleteAccountValidator;
            _updateAccountValidator = updateAccountValidator;
        }

        [EnableRateLimiting("registerPerIp")]
        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDto>> Register([FromBody] CreateUserDto createUserDto)
        {
            var validationResult = await _createUserValidator.ValidateAsync(createUserDto);
            if (!validationResult.IsValid)
            {
                return BadRequest(new { message = "Validation failed", errors = validationResult.Errors.Select(error => error.ErrorMessage)});
            }

            try
            {
                var result = await _authService.RegisterAsync(createUserDto);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException postgresEx && postgresEx.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                return Conflict(new { message = "Username or email already exists."});
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering a user.");
                return StatusCode(500, new { message = "An unexpected error occurred."});
            }
        }

        [EnableRateLimiting("loginPerIp")]
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginUserDto loginUserDto)
        {
            var validationResult = await _loginValidator.ValidateAsync(loginUserDto);
            if (!validationResult.IsValid)
            {
                return BadRequest(new { message = "Validation failed", errors = validationResult.Errors.Select(error => error.ErrorMessage)});
            }

            try
            {
                var result =await _authService.LoginAsync(loginUserDto);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging in a user.");
                return StatusCode(500, new { message = "An unexpected error occurred."});
            }
        }

        [AllowAnonymous]
        [EnableRateLimiting("refreshPerIp")]
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto refreshTokenRequestDto)
        {
            var validationResult = await _refreshTokenValidator.ValidateAsync(refreshTokenRequestDto);
            if (!validationResult.IsValid)
            {
                return BadRequest(new { message = "Validation failed", errors = validationResult.Errors.Select(error => error.ErrorMessage)});
            }

            try
            {
                var result = await _authService.RefreshTokenAsync(refreshTokenRequestDto);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing a token.");
                return StatusCode(500, new { message = "An unexpected error occurred."});
            }
        }

        [Authorize]
        [HttpGet("account")]
        public async Task<ActionResult<AccountResponseDto>> GetMyAccount()
        {
            var userIdClaims = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (!Guid.TryParse(userIdClaims, out var userId))
            {
                return Unauthorized(new { message = "Invalid user ID in token." });
            }

            try
            {
                var result = await _authService.GetAccountAsync(userId);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "User not found." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving account.");
                return StatusCode(500, new { message = "An unexpected error occurred."});
            }
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var authorization = Request.Headers.Authorization.ToString();
            if (string.IsNullOrWhiteSpace(authorization) || !authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return Unauthorized(new { message = "A Bearer access token is required."});
            }

            var accessToken = authorization["Bearer ".Length..].Trim();
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                return Unauthorized(new { message = "A Bearer access token is required."});
            }

            try
            {
                await _authService.LogoutAsync(accessToken);
                return Ok(new { message = "Logged out successfully."});
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging out a user.");
                return StatusCode(500, new { message = "An unexpected error occurred."});
            }
        }

        [Authorize]
        [HttpPut("password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var validation = await _changePasswordValidator.ValidateAsync(dto);
            if (!validation.IsValid)
            {
                return BadRequest(new { message = "The provided data is not valid.", errors = validation.Errors.Select(e => e.ErrorMessage)});
            }

            var userIdValue = User.FindFirst("sub")?.Value?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdValue, out var userId))
            {
                return Unauthorized(new { message = "Invalid user ID in token."});
            }

            var authorization = Request.Headers.Authorization.ToString();
            if (!authorization.StartsWith( "Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return Unauthorized(new { message = "A valid access token is required."});
            }

            var accessToken = authorization["Bearer ".Length..].Trim();

            try
            {
                await _authService.ChangePasswordAsync(userId, dto, accessToken);
                return Ok(new { message = "Password changed successfully. Please log in again with your new password."});
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing the password of the user {UserId}", userId);
                return StatusCode(500, new { message = "An error occurred while changing the password."});
            }
        }

        [Authorize]
        [HttpDelete("account")]
        public async Task<IActionResult> DeleteAccount([FromBody] DeleteAccountDto dto)
        {
            var validation = await _deleteAccountValidator.ValidateAsync(dto);
            if (!validation.IsValid)
            {
                return BadRequest(new {message = "Data provided is not valid.", errors = validation.Errors.Select(e => e.ErrorMessage)});
            }

            var userIdValue = User.FindFirst("sub")?.Value?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdValue, out var userId))
            {
                return Unauthorized(new {message = "Invalid user ID in token."});
            }

            var authorization = Request.Headers.Authorization.ToString();
            if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return Unauthorized(new {message = "A valid access token is required."});
            }

            var accessToken = authorization["Bearer ".Length..].Trim();
            try
            {
                await _authService.DeleteAccountAsync(userId, dto, accessToken);
                return Ok(new { message = "Account deleted successfully."});
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting the account of the user {UserId}", userId);
                return StatusCode(500, new {message = "An error occurred while deleting the account."});
            }
        }

        [Authorize]
        [HttpPatch("account")]
        public async Task<IActionResult> UpdateAccount([FromBody] UpdateAccountDto dto)
        {
            var validation = await _updateAccountValidator.ValidateAsync(dto);
            if (!validation.IsValid)
            {
                return BadRequest(new { message = "Data provider are not valid.", errors = validation.Errors.Select(e => e.ErrorMessage)});
            }

            var userIdValue = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdValue, out var userId))
            {
                return Unauthorized(new { message = "Invalid user ID in token."});
            }

            try
            {
                var updatedAccount = await _authService.UpdateAccountAsync(userId, dto);
                return Ok(new { message = "Account updated successfully.", account = updatedAccount});
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException postgresEx && postgresEx.SqlState == Npgsql.PostgresErrorCodes.UniqueViolation)
            {
                return Conflict(new { message = "The username or email is already registered."});
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating the account of the user {UserId}", userId);
                return StatusCode(500, new { message = "An error occurred while updating the account."});
            }
        }
    }
}