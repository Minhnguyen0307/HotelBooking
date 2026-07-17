using System.Threading.Tasks;
using HotelBooking.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Controllers
{
    [Authorize(Roles = "Manager,Admin")]
    public class AdminCustomerController : Controller
    {
        private readonly ICustomerService _customerService;

        public AdminCustomerController(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        public async Task<IActionResult> Index()
        {
            var customers = await _customerService.GetAllCustomersAsync();
            return View(customers);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var (success, error) = await _customerService.ToggleCustomerActiveStatusAsync(id);
            if (!success)
            {
                TempData["ErrorMessage"] = error;
            }
            else
            {
                TempData["SuccessMessage"] = "Đã cập nhật trạng thái hoạt động của khách hàng.";
            }

            return RedirectToAction("Index");
        }
    }
}
