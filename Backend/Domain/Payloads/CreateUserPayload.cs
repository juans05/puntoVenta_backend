using System.ComponentModel.DataAnnotations;

namespace Domain.Payloads
{
    public class CreateUserPayload
    {
        [Required]
        public string FirstName { get; set; }

        public string? LastName { get; set; }

        [Required]
        public string UserName { get; set; }

        public string? Email { get; set; }

        [Required]
        public string Password { get; set; }

        public string? Phone { get; set; }

        public string? Avatar { get; set; }

    }
}
