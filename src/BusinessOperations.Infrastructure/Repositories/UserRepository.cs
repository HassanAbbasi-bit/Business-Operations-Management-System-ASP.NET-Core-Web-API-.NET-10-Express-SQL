using BusinessOperations.Application.Interfaces.Repositories;
using BusinessOperations.Domain.Entities;
using BusinessOperations.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BusinessOperations.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.Employee)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        string normalized = email.Trim().ToLowerInvariant();
        return await _context.Users
            .Include(u => u.Employee)
            .FirstOrDefaultAsync(u => u.Email == normalized, cancellationToken);
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        string normalized = email.Trim().ToLowerInvariant();
        return await _context.Users
            .AnyAsync(u => u.Email == normalized, cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _context.Users.AddAsync(user, cancellationToken);
    }

    public void Update(User user)
    {
        _context.Users.Update(user);
    }
}
