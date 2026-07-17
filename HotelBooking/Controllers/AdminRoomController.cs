using HotelBooking.Application.DTOs;
using HotelBooking.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize(Roles = "Manager,Admin")]
public class AdminRoomController : Controller
{
    private readonly IRoomManageService _roomManageService;
    private readonly IRoomService _roomService;

    public AdminRoomController(IRoomManageService roomManageService, IRoomService roomService)
    {
        _roomManageService = roomManageService;
        _roomService = roomService;
    }

    public async Task<IActionResult> Index()
    {
        var rooms = await _roomManageService.GetAllRoomsAsync();
        return View(rooms);
    }

    public async Task<IActionResult> Create()
    {
        ViewBag.RoomTypes = await _roomService.GetAllRoomTypesAsync();
        return View(new RoomUpsertDto());
    }

    [HttpPost]
    public async Task<IActionResult> Create(RoomUpsertDto dto)
    {
        var (success, error) = await _roomManageService.SaveRoomAsync(dto);
        if (!success)
        {
            ModelState.AddModelError("", error!);
            ViewBag.RoomTypes = await _roomService.GetAllRoomTypesAsync();
            return View(dto);
        }

        TempData["SuccessMessage"] = "Đã thêm phòng mới.";
        return RedirectToAction("Index");
    }

    public async Task<IActionResult> Edit(int id)
    {
        var dto = await _roomManageService.GetRoomForEditAsync(id);
        if (dto == null) return NotFound();

        ViewBag.RoomTypes = await _roomService.GetAllRoomTypesAsync();
        return View(dto);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(RoomUpsertDto dto)
    {
        var (success, error) = await _roomManageService.SaveRoomAsync(dto);
        if (!success)
        {
            ModelState.AddModelError("", error!);
            ViewBag.RoomTypes = await _roomService.GetAllRoomTypesAsync();
            return View(dto);
        }

        TempData["SuccessMessage"] = "Đã cập nhật phòng.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> UpdateStatus(int roomId, string status)
    {
        var (success, error) = await _roomManageService.UpdateStatusAsync(roomId, status);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = success ? "Đã cập nhật trạng thái." : error;
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var (success, error) = await _roomManageService.DeleteRoomAsync(id);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = success ? "Đã xóa phòng thành công." : error;
        return RedirectToAction("Index");
    }
}