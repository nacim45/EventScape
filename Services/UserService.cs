using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using soft20181_starter.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace soft20181_starter.Services
{
    public interface IUserService
    {
        Task<AppUser> CreateUserAsync(AppUser user, string password, string role);
        Task<AppUser> UpdateUserAsync(string userId, AppUser updatedUser);
        Task<bool> DeleteUserAsync(string userId);
        Task<AppUser> GetUserByIdAsync(string userId);
        Task<AppUser> GetUserByEmailAsync(string email);
        Task<List<AppUser>> GetAllUsersAsync();
        Task<List<AppUser>> GetUsersByRoleAsync(string role);
        Task<bool> UserExistsAsync(string email);
        Task<bool> UpdateUserRoleAsync(string userId, string newRole);
    }

    public class UserService : IUserService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly EventAppDbContext _context;
        private readonly ILogger<UserService> _logger;

        public UserService(
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager,
            EventAppDbContext context,
            ILogger<UserService> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _logger = logger;
        }

        public async Task<AppUser> CreateUserAsync(AppUser user, string password, string role)
        {
            _logger.LogInformation("Creating user: {Email}", user.Email);

            try
            {
                // Begin transaction
                await using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Create user
                    var result = await _userManager.CreateAsync(user, password);
                    if (!result.Succeeded)
                    {
                        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                        _logger.LogError("Failed to create user {Email}: {Errors}", user.Email, errors);
                        throw new InvalidOperationException($"Failed to create user: {errors}");
                    }

                    _logger.LogInformation("User created successfully: {UserId}", user.Id);

                    // Assign role
                    if (!string.IsNullOrEmpty(role))
                    {
                        var roleResult = await _userManager.AddToRoleAsync(user, role);
                        if (!roleResult.Succeeded)
                        {
                            _logger.LogWarning("Failed to assign role {Role} to user {UserId}", role, user.Id);
                        }
                        else
                        {
                            _logger.LogInformation("Role {Role} assigned to user {UserId}", role, user.Id);
                        }
                    }

                    // Create audit log
                    var auditLog = new AuditLog
                    {
                        EntityName = "User",
                        EntityId = user.Id,
                        Action = "Create",
                        UserId = "System",
                        Changes = $"Created user: {user.Name} {user.Surname} (ID: {user.Id}) with role: {role}",
                        Timestamp = DateTime.UtcNow
                    };
                    await _context.AuditLogs.AddAsync(auditLog);
                    await _context.SaveChangesAsync();

                    // Commit transaction
                    await transaction.CommitAsync();
                    _logger.LogInformation("User creation transaction committed: {UserId}", user.Id);

                    return user;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user {Email}", user.Email);
                throw;
            }
        }

        public async Task<AppUser> UpdateUserAsync(string userId, AppUser updatedUser)
        {
            _logger.LogInformation("Updating user: {UserId}", userId);

            try
            {
                var existingUser = await _userManager.FindByIdAsync(userId);
                if (existingUser == null)
                {
                    throw new InvalidOperationException($"User with ID {userId} not found");
                }

                // Update properties
                existingUser.Name = updatedUser.Name;
                existingUser.Surname = updatedUser.Surname;
                existingUser.Email = updatedUser.Email;
                existingUser.UserName = updatedUser.Email;
                existingUser.PhoneNumber = updatedUser.PhoneNumber;
                existingUser.Role = updatedUser.Role;
                existingUser.UpdatedAt = DateTime.UtcNow;
                existingUser.UpdatedBy = "System";

                var result = await _userManager.UpdateAsync(existingUser);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to update user: {errors}");
                }

                // Create audit log
                var auditLog = new AuditLog
                {
                    EntityName = "User",
                    EntityId = userId,
                    Action = "Update",
                    UserId = "System",
                    Changes = $"Updated user: {existingUser.Name} {existingUser.Surname}",
                    Timestamp = DateTime.UtcNow
                };
                await _context.AuditLogs.AddAsync(auditLog);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User updated successfully: {UserId}", userId);
                return existingUser;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> DeleteUserAsync(string userId)
        {
            _logger.LogInformation("Deleting user: {UserId}", userId);

            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User not found: {UserId}", userId);
                    return false;
                }

                var result = await _userManager.DeleteAsync(user);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogError("Failed to delete user {UserId}: {Errors}", userId, errors);
                    return false;
                }

                // Create audit log
                var auditLog = new AuditLog
                {
                    EntityName = "User",
                    EntityId = userId,
                    Action = "Delete",
                    UserId = "System",
                    Changes = $"Deleted user: {user.Name} {user.Surname}",
                    Timestamp = DateTime.UtcNow
                };
                await _context.AuditLogs.AddAsync(auditLog);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User deleted successfully: {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", userId);
                return false;
            }
        }

        public async Task<AppUser> GetUserByIdAsync(string userId)
        {
            return await _userManager.FindByIdAsync(userId);
        }

        public async Task<AppUser> GetUserByEmailAsync(string email)
        {
            return await _userManager.FindByEmailAsync(email);
        }

        public async Task<List<AppUser>> GetAllUsersAsync()
        {
            return await _userManager.Users.ToListAsync();
        }

        public async Task<List<AppUser>> GetUsersByRoleAsync(string role)
        {
            var usersInRole = await _userManager.GetUsersInRoleAsync(role);
            return usersInRole.ToList();
        }

        public async Task<bool> UserExistsAsync(string email)
        {
            return await _userManager.FindByEmailAsync(email) != null;
        }

        public async Task<bool> UpdateUserRoleAsync(string userId, string newRole)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return false;
                }

                // Get current roles
                var currentRoles = await _userManager.GetRolesAsync(user);
                
                // Remove current roles
                if (currentRoles.Any())
                {
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);
                }

                // Add new role
                var result = await _userManager.AddToRoleAsync(user, newRole);
                if (result.Succeeded)
                {
                    user.Role = newRole;
                    user.UpdatedAt = DateTime.UtcNow;
                    await _userManager.UpdateAsync(user);

                    // Create audit log
                    var auditLog = new AuditLog
                    {
                        EntityName = "User",
                        EntityId = userId,
                        Action = "UpdateRole",
                        UserId = "System",
                        Changes = $"Updated role to {newRole} for user: {user.Name} {user.Surname}",
                        Timestamp = DateTime.UtcNow
                    };
                    await _context.AuditLogs.AddAsync(auditLog);
                    await _context.SaveChangesAsync();

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating role for user {UserId}", userId);
                return false;
            }
        }
    }
} 