using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WebAPI_LoginApp.Data;
using WebAPI_LoginApp.DTOs;
using WebAPI_LoginApp.Models;
using WebAPI_LoginApp.Sevices;

namespace WebAPI_LoginApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        //Create a readonly field for dbcontext
        private readonly AppDbContext dbcontext;
        private readonly IConfiguration configuration;
        private readonly EmailService emailService;

        //Parameterized constructor which an instance of AppDbContext
        public UsersController(AppDbContext _dbcontext, IConfiguration _configuration, EmailService _emailService)
        {
            //Here we are assigning the value which we have in _dbcontext to our readonly field dbcontext
            this.dbcontext = _dbcontext; 
            this.configuration = _configuration;
            this.emailService = _emailService;
        }
        [HttpPost]
        [Route("Registration")]
        public IActionResult Registration(UserDTO userDTO)
        {
            if(!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            //Using below line of code performing a query on the Users table in database using Entity Framework Core
            var objUser = dbcontext.Users.FirstOrDefault(x => x.Email == userDTO.Email);
            if (objUser == null)
            {
                //Using the PasswordHasher<TUser> class provided by ASP.NET Core Identity for password hashing
                var passwordHasher = new PasswordHasher<User>();
                var newUser = new User
                {
                    FirstName = userDTO.FirstName,
                    LastName = userDTO.LastName,
                    Email = userDTO.Email,
                    ResetPasswordToken = null,
                    ResetPasswordTokenExpiry = null
                };
                //Hashes the plain - text password before saving it to the database
                newUser.Password = passwordHasher.HashPassword(newUser, userDTO.Password);

                dbcontext.Users.Add(newUser);
                dbcontext.SaveChanges();
                return Ok("User Registered Successfully");
            }
            else
            {
                return BadRequest("User already exist with same Email address");
            }
        }
        [HttpPost]
        [Route("Login")]
        public IActionResult Login(LoginDTO loginDTO)
        {
            var user = dbcontext.Users.FirstOrDefault( x => x.Email == loginDTO.Email);
            if (user != null)
            {
                var passwordHasher = new PasswordHasher<User>();
                //Compares the hashed password stored in the database with the plain-text password entered by the user during login
                var verificationResult = passwordHasher.VerifyHashedPassword(user, user.Password, loginDTO.Password);

                if (verificationResult == PasswordVerificationResult.Success)
                {
                    #region JWT Authentication Code
                    var claims = new[]
                    {
                       new Claim(JwtRegisteredClaimNames.Sub, configuration["Jwt:Subject"]),
                       new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                       new Claim("UserId", user.UserId.ToString()),
                       new Claim("Email", user.Email.ToString())
                    };
                    var Key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]));
                    var signIn = new SigningCredentials(Key, SecurityAlgorithms.HmacSha256);

                    //Code to generate Token
                    var token = new JwtSecurityToken(
                        configuration["Jwt:Issuer"],
                        configuration["Jwt:Audience"],
                        claims,
                        expires: DateTime.UtcNow.AddMinutes(60), //Token active for 60 minutes
                        signingCredentials: signIn
                        );
                    string tokenValue = new JwtSecurityTokenHandler().WriteToken(token);
                    #endregion

                    //Mapping values of User Model class to UserResponseDTO class
                    var userResponse = new UserResponseDTO
                    {
                        UserId = user.UserId,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Email = user.Email,
                        CreatedOn = user.CreatedOn
                    };
                    return Ok(new { Token = tokenValue, User = userResponse });
                }
            }
            return NoContent();
        }
        [HttpGet]
        [Route("GetUsers")]
        public IActionResult GetUsers()
        {
            // Fetch all users and map to UserResponseDTO
            var users = dbcontext.Users.Select(user => new UserResponseDTO
            {
                UserId = user.UserId,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                CreatedOn = user.CreatedOn
            }).ToList();

            return Ok(users);
        }
        [HttpGet]
        [Route("GetUser")]
        [Authorize]
        public IActionResult GetUser(int id)
        {
            var user = dbcontext.Users.FirstOrDefault(x => x.UserId == id);
            if(user != null)
            {
                var userResponse = new UserResponseDTO
                {
                    UserId = user.UserId,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    CreatedOn = user.CreatedOn
                };
                return Ok(userResponse);
            }
            else
            {
                return BadRequest("User does not exist");
            }
        }
        [HttpPost]
        [Route("ForgotPassword")]
        public async Task<IActionResult> ForgotPassword(PasswordResetRequestDTO request)
        {
            var user = dbcontext.Users.FirstOrDefault(x => x.Email == request.Email);
            if(user==null)
            {
                return NotFound("Email not registered, please register");
            }

            //Generate the request token and save it to the Db
            var claims = new[]
            {
               new Claim(JwtRegisteredClaimNames.Sub, configuration["Jwt:Subject"]),
               new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
               new Claim("User", user.UserId.ToString()),
               new Claim("Email", user.Email.ToString()),
            };

            var Key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]));
            var signIn = new SigningCredentials(Key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                configuration["Jwt:Issuer"],
                configuration["Jwt:Audience"],
                claims,
                expires: DateTime.UtcNow.AddMinutes(2),
                signingCredentials: signIn);

            var resetToken = new JwtSecurityTokenHandler().WriteToken(token);

            user.ResetPasswordToken = resetToken;
            user.ResetPasswordTokenExpiry = DateTime.UtcNow.AddMinutes(2);
            dbcontext.SaveChanges();

            var resetLink = Url.Action("ResetPassword", "UserController", new { token = user.ResetPasswordToken }, Request.Scheme);
            var emailBody = $"Please reset your Password by clicking  <a href=\"{resetLink}\">here</a>.";

            //Send the email
            await emailService.SendEmailAsyn(user.Email, "Password Reset", emailBody);

            return Ok($"Password reset link has been sent to your email : Token : {resetToken}");     
        }
        [HttpPost]
        [Route("ResetPassword")]
        public async Task<IActionResult> ResetPassword(ResetPasswordDTO resetPasswordDTO)
        {
            // Find the user by the reset token
            var user = dbcontext.Users.FirstOrDefault(x => x.ResetPasswordToken == resetPasswordDTO.Token);

            if (user == null || user.ResetPasswordTokenExpiry < DateTime.UtcNow)
            {
                return BadRequest("Invalid or expired token.");
            }

            // Hash the new password
            var passwordHasher = new PasswordHasher<User>();
            user.Password = passwordHasher.HashPassword(user, resetPasswordDTO.NewPassword);

            // Clear the reset token and expiry fields
            user.ResetPasswordToken = null;
            user.ResetPasswordTokenExpiry = null;

            // Save changes to the database
            dbcontext.SaveChanges();

            return Ok("Password has been reset successfully.");
        }
    }
}
