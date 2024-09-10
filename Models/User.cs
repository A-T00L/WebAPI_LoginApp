namespace WebAPI_LoginApp.Models
{
    public class User
    {
        public int UserId { get; set; } = 0;
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public int IsActive { get; set; } = 1;
        public DateTime CreatedOn { get; set; } = DateTime.Now;

        //Field for password reset
        public string? ResetPasswordToken { get; set; }
        public DateTime? ResetPasswordTokenExpiry { get; set; }
    }
}