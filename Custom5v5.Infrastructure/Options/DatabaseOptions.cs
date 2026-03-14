namespace Custom5v5.Infrastructure.Options;

public class DatabaseOptions
{
    public string Host { get; set; } = default!;
    public int Port { get; set; }
    public string Database { get; set; } = default!;
    public string Username { get; set; } = default!;
    public string Password { get; set; } = default!;
}