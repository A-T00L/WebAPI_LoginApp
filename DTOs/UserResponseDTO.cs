namespace WebAPI_LoginApp.DTOs
{
    public class UserResponseDTO
    {
        public int UserId { get; set; } = 0;
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public DateTime CreatedOn { get; set; }
    }
}
