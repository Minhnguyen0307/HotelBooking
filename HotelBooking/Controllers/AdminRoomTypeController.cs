using System.Threading.Tasks;
using HotelBooking.Application.Interfaces;
using HotelBooking.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Controllers
{
    [Authorize(Roles = "Manager,Admin")]
    public class AdminRoomTypeController : Controller
    {
        private readonly IRoomTypeService _roomTypeService;

        public AdminRoomTypeController(IRoomTypeService roomTypeService)
        {
            _roomTypeService = roomTypeService;
        }

        public async Task<IActionResult> Index()
        {
            var types = await _roomTypeService.GetAllRoomTypesAsync();
            return View(types);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new RoomType { MaxGuests = 2 });
        }

        [HttpPost]
        public async Task<IActionResult> Create(RoomType roomType)
        {
            if (!ModelState.IsValid) return View(roomType);

            var (success, error) = await _roomTypeService.SaveRoomTypeAsync(roomType);
            if (!success)
            {
                ModelState.AddModelError("", error!);
                return View(roomType);
            }

            TempData["SuccessMessage"] = "Đã thêm loại phòng mới thành công.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var roomType = await _roomTypeService.GetRoomTypeByIdAsync(id);
            if (roomType == null) return NotFound();
            return View(roomType);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(RoomType roomType)
        {
            if (!ModelState.IsValid) return View(roomType);

            var (success, error) = await _roomTypeService.SaveRoomTypeAsync(roomType);
            if (!success)
            {
                ModelState.AddModelError("", error!);
                return View(roomType);
            }

            TempData["SuccessMessage"] = "Đã cập nhật loại phòng thành công.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var (success, error) = await _roomTypeService.DeleteRoomTypeAsync(id);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = success ? "Đã xóa loại phòng thành công." : error;
            return RedirectToAction("Index");
        }
    }
}
