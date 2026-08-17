using Microsoft.EntityFrameworkCore;
using Seatsure.DAL.Repositories.Interfaces;
using Seatsure.Domain;

namespace Seatsure.DAL.Repositories.Impl;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context) => _context = context;

    public async Task<User?> GetByIdAsync(Guid id) =>
        await _context.Users.FindAsync(id); // implement FindAsync for better performance

    // firstOrDefaultAsync 
    // if found return user, else return null 
    
    public async Task<User?> GetByEmailAsync(string email) =>

        await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

    public async Task AddAsync(User user) =>
        await _context.Users.AddAsync(user);


    // why implement repository, while i'm already have DbContext 
    // 1. avoid code from being tightly-coupled 
    // 2. EntityRepoistory will be used in service, depend on Repository, is better than depending on DbContext 
    // 
}




