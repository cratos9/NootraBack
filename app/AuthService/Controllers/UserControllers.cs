using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using FluentValidation;
using AuthService.Dtos;
using AuthService.Services;
using Microsoft.AspNetCore.Authorization;

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

        public UserController(IAuthService authService, IValidator<CreateUserDto> createUserValidator, IValidator<RefreshTokenRequestDto> refreshTokenValidator, IValidator<LoginUserDto> loginValidator)
        {
            _authService = authService;
            _createUserValidator = createUserValidator;
            _refreshTokenValidator = refreshTokenValidator;
            _loginValidator = loginValidator;
        }

        [EnableRateLimiting("RegisterPerIp")]
        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDto>> Register([FromBody] CreateUserDto createUserDto)
        {
            try
            {
                var validationResult = await _createUserValidator.ValidateAsync(createUserDto);
                if (!validationResult.IsValid)
                {
                    return BadRequest( new { message = "Validation failed", errors = validationResult.Errors.Select(error => error.ErrorMessage) });
                }

                var result = await _authService.RegisterAsync(createUserDto);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            } catch (Exception)
            {
                return BadRequest(new { message = "An error occurred while processing the request." });
            }
        }

        [EnableRateLimiting("LoginPerIp")]
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginUserDto loginUserDto)
        {
            try
            {
                var validationResult = await _loginValidator.ValidateAsync(loginUserDto);
                if (!validationResult.IsValid)
                {
                    return BadRequest( new { message = "Validation failed", errors = validationResult.Errors.Select(error => error.ErrorMessage) });
                }

                var result = await _authService.LoginAsync(loginUserDto);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            } catch (Exception)
            {
                return BadRequest(new { message = "An error occurred while processing the request." });
            }
        }

        [EnableRateLimiting("refreshPerIp")]
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh ([FromBody] RefreshTokenRequestDto refreshTokenRequestDto)
        {
            try
            {
                var result = await _authService.RefreshTokenAsync(refreshTokenRequestDto);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception)
            {
                return BadRequest(new { message = "An error occurred while processing the request." });
            }
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var authorization = Request.Headers.Authorization.ToString();
            if (authorization.StartsWith("Bearer "))
            {
                return Unauthorized(new { message = "Access token is required for logout." });
            }

            var token = authorization["Bearer ".Length..];
            await _authService.LogoutAsync(token);
            return Ok(new { message = "Logged out successfully." });
        }
    }
}