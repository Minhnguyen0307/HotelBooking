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
    }
}
