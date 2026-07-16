using HotelBooking.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

public class HomeController : Controller
{
    private readonly IRoomService _roomService;

    public HomeController(IRoomService roomService)
    {
        _roomService = roomService;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.RoomTypes = await _roomService.GetAllRoomTypesAsync();

        var featuredRooms = await _roomService.SearchAvailableRoomsAsync(new HotelBooking.Application.DTOs.RoomSearchDto
        {
            CheckInDate = DateOnly.FromDateTime(DateTime.Today),
            CheckOutDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1))
        });

        return View(featuredRooms.Take(6).ToList());
    }

    public IActionResult Privacy()
    {
        return View();
    }
}
