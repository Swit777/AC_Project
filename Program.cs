using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<UserStore>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.MapGet("/api/users", (UserStore store) =>
    Results.Ok(store.GetAll()));

app.MapPost("/api/register", (RegisterRequest req, UserStore store) =>
{
    if (string.IsNullOrWhiteSpace(req.Name) ||
        string.IsNullOrWhiteSpace(req.Email) ||
        string.IsNullOrWhiteSpace(req.Password))
        return Results.BadRequest(new { error = "All fields are required." });

    if (store.EmailExists(req.Email))
        return Results.Conflict(new { error = "Email already registered." });

    var user = new User(Guid.NewGuid(), req.Name, req.Email);
    store.Add(user);
    return Results.Created($"/api/users/{user.Id}", user);
});

app.Run();

record RegisterRequest(string Name, string Email, string Password);
record User(Guid Id, string Name, string Email);

class UserStore
{
    private readonly ConcurrentDictionary<string, User> _users = new();

    public void Add(User user) => _users[user.Email] = user;
    public bool EmailExists(string email) => _users.ContainsKey(email);
    public IEnumerable<User> GetAll() => _users.Values;
}