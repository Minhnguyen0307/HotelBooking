using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace HotelBooking.Application.DTOs
{
    public class ReviewItemDto
    {
        public int ReviewId { get; set; }
        public int RoomId { get; set; }
        public string? RoomNumber { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<string> ImageUrls { get; set; } = new();
        public bool IsVerifiedStay { get; set; }
        public string? StaffResponse { get; set; }
        public string? StaffResponderName { get; set; }
        public DateTime? RespondedAt { get; set; }
    }

    public class ReplyReviewDto
    {
        public int ReviewId { get; set; }
        public int? RoomId { get; set; }
        public string Response { get; set; } = string.Empty;
    }

    public class CreateReviewDto
    {
        public int RoomId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public List<IFormFile>? Images { get; set; }
    }

    public class RoomReviewsSummaryDto
    {
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public Dictionary<int, int> RatingCounts { get; set; } = new()
        {
            { 5, 0 }, { 4, 0 }, { 3, 0 }, { 2, 0 }, { 1, 0 }
        };
        public List<ReviewItemDto> Reviews { get; set; } = new();
    }
}

