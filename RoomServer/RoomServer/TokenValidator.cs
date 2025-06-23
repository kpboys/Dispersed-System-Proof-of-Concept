using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

public static class TokenValidator
{
    public static async Task<bool> ValidateJwt(string token, string? role = null)
    {
        var handler = new JwtSecurityTokenHandler();
        TokenValidationParameters param = new TokenValidationParameters()
        {
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidateIssuer = false,
            ValidateAudience = false,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("JWT_SECRET_KEY"))),
            RoleClaimType = ClaimTypes.Role // This ensures that role claims are used for authorization
        };
        try
        {
            ClaimsPrincipal validatedResult = handler.ValidateToken(token, param, out SecurityToken outToken);

            if (role != null && !validatedResult.IsInRole(role))
                return false;

            string? username = validatedResult.FindFirst(ClaimTypes.Name)?.Value;
            string? sessionId = validatedResult.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (username == null || sessionId == null)
                return false;

            return await CheckIdAsync(username, sessionId);
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static async Task<bool> CheckIdAsync(string username, string sessionId)
    {
        using var client = new HttpClient();
        string url = Environment.GetEnvironmentVariable("SERVICE_ALIAS_SESSION");
        string path = "api/checksessionid";
        string checkSessionIdRoute = url + path;
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, checkSessionIdRoute);
        request.Content = CreateJson(new IdCheckDto() { Username = username, SessionId = sessionId });
        try
        {
            HttpResponseMessage response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode)
                return true;
            else
                return false;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static StringContent CreateJson(object jsonObject)
    {
        string jsonString = JsonSerializer.Serialize(jsonObject);
        return new StringContent(jsonString, encoding: Encoding.UTF8, "application/json");
    }

    public class IdCheckDto
    {
        public required string Username { get; set; }
        public required string SessionId { get; set; }
    }

}

