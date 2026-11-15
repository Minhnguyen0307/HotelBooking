using System.Text.Json;
using System.Text.Json.Nodes;
using HotelBooking.Application.Services;
using HotelBooking.Application.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace HotelBooking.Controllers.Api;

[ApiController]
[Authorize(Roles = "Manager,Admin")]
[Route("api/admin/settings")]
public class AdminSettingsApiController : ControllerBase
{
    private readonly HotelPolicies _policies;
    private readonly IWebHostEnvironment _env;
    private readonly HotelBookingAutomationWorker _worker;

    public AdminSettingsApiController(
        IOptions<HotelPolicies> policies,
        IWebHostEnvironment env,
        HotelBookingAutomationWorker worker)
    {
        _policies = policies.Value;
        _env = env;
        _worker = worker;
    }

    [HttpGet("pricing-policy")]
    public IActionResult GetPricingPolicy()
    {
        return Ok(new
        {
            _policies.StandardGuestsPerRoom,
            _policies.ExtraAdultFeePerNight,
            _policies.ExtraChildFeePerNight,
            _policies.WeekendMarkupPercent,
            _policies.HolidayMarkupPercent,
            Holidays = _policies.Holidays ?? new List<string>()
        });
    }

    [HttpPut("pricing-policy")]
    public async Task<IActionResult> UpdatePricingPolicy([FromBody] UpdatePolicyDto dto)
    {
        if (dto.WeekendMarkupPercent < 0 || dto.WeekendMarkupPercent > 200)
            return BadRequest(new { message = "Tỷ lệ tăng giá cuối tuần không hợp lệ (0% - 200%)." });

        if (dto.HolidayMarkupPercent < 0 || dto.HolidayMarkupPercent > 200)
            return BadRequest(new { message = "Tỷ lệ tăng giá ngày lễ không hợp lệ (0% - 200%)." });

        _policies.WeekendMarkupPercent = dto.WeekendMarkupPercent;
        _policies.HolidayMarkupPercent = dto.HolidayMarkupPercent;
        _policies.Holidays = dto.Holidays ?? new List<string>();

        // Persist to appsettings.json
        try
        {
            var appSettingsPath = Path.Combine(_env.ContentRootPath, "appsettings.json");
            if (System.IO.File.Exists(appSettingsPath))
            {
                var jsonString = await System.IO.File.ReadAllTextAsync(appSettingsPath);
                var jsonObj = JsonNode.Parse(jsonString);
                if (jsonObj != null)
                {
                    var policiesNode = jsonObj["HotelPolicies"]?.AsObject();
                    if (policiesNode != null)
                    {
                        policiesNode["WeekendMarkupPercent"] = dto.WeekendMarkupPercent;
                        policiesNode["HolidayMarkupPercent"] = dto.HolidayMarkupPercent;

                        var holidaysArray = new JsonArray();
                        foreach (var h in _policies.Holidays)
                        {
                            holidaysArray.Add(h);
                        }
                        policiesNode["Holidays"] = holidaysArray;

                        await System.IO.File.WriteAllTextAsync(appSettingsPath, jsonObj.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
                    }
                }
            }
        }
        catch
        {
            // If file write fails due to permission, in-memory updates still work
        }

        return Ok(new { message = "Đã cập nhật chính sách định giá động thành công." });
    }

    [HttpPost("trigger-automation")]
    public async Task<IActionResult> TriggerAutomation()
    {
        try
        {
            var (reminders, thanks) = await _worker.TriggerManualRunAsync();
            return Ok(new
            {
                message = $"Đã thực thi gửi email tự động: {reminders} email nhắc nhở check-in, {thanks} email cảm ơn sau check-out.",
                remindersSent = reminders,
                thankYouSent = thanks
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Lỗi khi chạy automation: {ex.Message}" });
        }
    }

    public class UpdatePolicyDto
    {
        public double WeekendMarkupPercent { get; set; }
        public double HolidayMarkupPercent { get; set; }
        public List<string>? Holidays { get; set; }
    }
}
