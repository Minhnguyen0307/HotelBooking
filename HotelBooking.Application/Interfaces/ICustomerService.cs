using System.Collections.Generic;
using System.Threading.Tasks;
using HotelBooking.Infrastructure;

namespace HotelBooking.Application.Interfaces
{
    public interface ICustomerService
    {
        Task<List<User>> GetAllCustomersAsync();
        Task<List<User>> GetAllUsersAsync();
        Task<List<Role>> GetAllRolesAsync();
        Task<(bool Success, string? Error)> ChangeUserRoleAsync(int userId, int newRoleId);
        Task<(bool Success, string? Error)> ToggleCustomerActiveStatusAsync(int userId);
        Task<List<User>> GetStaffAsync();
        Task<(bool Success, string? Error)> CreateStaffAsync(string fullName, string email, string? phone, string roleName, string password);
        Task<(bool Success, string? Error)> UpdateStaffAsync(int userId, int actorId, string fullName, string? phone, string roleName, bool isActive);
    }
}
