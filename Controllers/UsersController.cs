using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WebAPI_LoginApp.Data;
using WebAPI_LoginApp.DTOs;
using WebAPI_LoginApp.Models;

namespace WebAPI_LoginApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        //Create a readonly field for dbcontext
        private readonly AppDbContext dbcontext;
        private readonly IConfiguration configuration;

        //Parameterized constructor which an instance of AppDbContext
        public UsersController(AppDbContext _dbcontext, IConfiguration _configuration)
        {
            //Here we are assigning the value which we have in _dbcontext to our readonly field dbcontext
            this.dbcontext = _dbcontext; 
            this.configuration = _configuration;
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
                    Email = userDTO.Email
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
    }
}
