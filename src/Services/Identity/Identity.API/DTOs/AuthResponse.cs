namespace Identity.API.DTOs;

public class AuthResponse
{
    public string AccessToken { get; set; } = null!;
    public string RefreshToken { get; set; } = null!;

    public int ExpiresIn { get; set; }

    public string TokenType { get; set; } = "Bearer";
    public string UserId { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public IList<string> Roles { get; set; } = new List<string>();
}
