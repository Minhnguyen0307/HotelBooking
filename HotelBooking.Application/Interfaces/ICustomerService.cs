using System.Collections.Generic;
using System.Threading.Tasks;
using HotelBooking.Infrastructure;

namespace HotelBooking.Application.Interfaces
{
    public interface ICustomerService
    {
        Task<List<User>> GetAllCustomersAsync();
        Task<(bool Success, string? Error)> ToggleCustomerActiveStatusAsync(int userId);
    }
}
