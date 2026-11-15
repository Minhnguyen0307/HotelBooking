using HotelBooking.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Controllers.Api;

[ApiController]
[Authorize(Roles = "Manager,Admin")]
[Route("api/admin/reviews")]
public class AdminReviewsApiController : ControllerBase
{
    private readonly IReviewService _reviewService;

    public AdminReviewsApiController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var reviews = await _reviewService.GetAllReviewsAsync();
        return Ok(reviews);
    }

    [HttpPost("{id:int}/reply")]
    public async Task<IActionResult> Reply(int id, [FromBody] ReplyReviewRequest request)
    {
        var staffUserId = ApiUser.Id(User);
        var (success, error) = await _reviewService.ReplyReviewAsync(staffUserId, id, request.Response);
        if (!success) return BadRequest(new { message = error });
        return Ok(new { message = "Đã gửi phản hồi đánh giá." });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var (success, error) = await _reviewService.DeleteReviewAsync(id);
        if (!success) return BadRequest(new { message = error });
        return Ok(new { message = "Đã xóa bài đánh giá." });
    }

    public class ReplyReviewRequest
    {
        public string Response { get; set; } = string.Empty;
    }
}
