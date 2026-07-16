using HotelBooking.Application.DTOs;
using HotelBooking.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

public class RoomController : Controller
{
    private readonly IRoomService _roomService;

    public RoomController(IRoomService roomService)
    {
        _roomService = roomService;
    }

    [HttpGet]
    public async Task<IActionResult> Search()
    {
        ViewBag.RoomTypes = await _roomService.GetAllRoomTypesAsync();
        return View(new RoomSearchDto());

    }

    [HttpPost]
    public async Task<IActionResult> Search(RoomSearchDto criteria)
    {
        ViewBag.RoomTypes = await _roomService.GetAllRoomTypesAsync();

        if (!ModelState.IsValid)
            return View(criteria);

        try
        {
            var rooms = await _roomService.SearchAvailableRoomsAsync(criteria);
            ViewBag.SearchCriteria = criteria;
            return View("SearchResults", rooms);
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(criteria);
        }
    }

    public async Task<IActionResult> Details(int id)
    {
        var room = await _roomService.GetRoomDetailAsync(id);
        if (room == null) return NotFound();
        return View(room);
    }

}