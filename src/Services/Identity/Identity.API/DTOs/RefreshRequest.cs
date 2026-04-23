using System.ComponentModel.DataAnnotations;

namespace Identity.API.DTOs;

public class RefreshRequest
{
    [Required]
    public string RefreshToken { get; set; } = null!;
}
