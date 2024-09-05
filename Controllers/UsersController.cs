using Microsoft.AspNetCore.Mvc;
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

        //Parameterized constructor which an instance of AppDbContext
        public UsersController(AppDbContext _dbcontext)
        {
            //Here we are assigning the value which we have in _dbcontext to our readonly field dbcontext
            this.dbcontext = _dbcontext; 
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
                dbcontext.Users.Add(new User
                {
                    FirstName = userDTO.FirstName,
                    LastName = userDTO.LastName,
                    Email = userDTO.Email,
                    Password = userDTO.Password
                });
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
            var user = dbcontext.Users.FirstOrDefault( x => x.Email == loginDTO.Email && x.Password == loginDTO.Password);
            if (user != null)
            {
                return Ok(user);
            }
            return NoContent();
        }
        [HttpGet]
        [Route("GetUsers")]
        public IActionResult GetUsers()
        {
            return Ok(dbcontext.Users.ToList());
        }
        [HttpGet]
        [Route("GetUser")]
        public IActionResult GetUser(int id)
        {
            var user = dbcontext.Users.FirstOrDefault( x => x.UserId == id);
            if(user != null)
            {
                return Ok(user);
            }
            else
            {
                return BadRequest("User does not exist");
            }
        }   
    }
}
