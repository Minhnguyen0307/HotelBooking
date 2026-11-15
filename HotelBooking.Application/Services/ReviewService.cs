using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HotelBooking.Application.DTOs;
using HotelBooking.Application.Interfaces;
using HotelBooking.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Services
{
    public class ReviewService : IReviewService
    {
        private readonly HotelBookingDbContext _context;

        public ReviewService(HotelBookingDbContext context)
        {
            _context = context;
        }

        public async Task<RoomReviewsSummaryDto> GetRoomReviewsSummaryAsync(int roomId)
        {
            try
            {
                var reviews = await _context.Reviews
                    .Include(r => r.Room)
                    .Include(r => r.User)
                    .Include(r => r.RespondedByUser)
                    .Include(r => r.ReviewImages)
                    .Where(r => r.RoomId == roomId)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();

                var verifiedUserIds = await _context.BookingRooms
                    .Where(br => br.RoomId == roomId && 
                        (br.Booking.Status == "CheckedOut" || br.Booking.Status == "CheckedIn" || br.Booking.Status == "Confirmed"))
                    .Select(br => br.Booking.CustomerId)
                    .Distinct()
                    .ToListAsync();

                var summary = new RoomReviewsSummaryDto
                {
                    TotalReviews = reviews.Count,
                    AverageRating = reviews.Any() ? Math.Round(reviews.Average(r => r.Rating), 1) : 0,
                    RatingCounts = new Dictionary<int, int>
                    {
                        { 5, reviews.Count(r => r.Rating == 5) },
                        { 4, reviews.Count(r => r.Rating == 4) },
                        { 3, reviews.Count(r => r.Rating == 3) },
                        { 2, reviews.Count(r => r.Rating == 2) },
                        { 1, reviews.Count(r => r.Rating == 1) }
                    },
                    Reviews = reviews.Select(r => new ReviewItemDto
                    {
                        ReviewId = r.ReviewId,
                        RoomId = r.RoomId,
                        RoomNumber = r.Room?.RoomNumber ?? "",
                        UserId = r.UserId,
                        UserName = r.User.FullName ?? "Khách hàng",
                        Rating = r.Rating,
                        Comment = r.Comment,
                        CreatedAt = r.CreatedAt,
                        IsVerifiedStay = verifiedUserIds.Contains(r.UserId),
                        StaffResponse = r.StaffResponse,
                        StaffResponderName = r.RespondedByUser?.FullName ?? (r.RespondedByUserId.HasValue ? "Lễ tân khách sạn" : null),
                        RespondedAt = r.RespondedAt,
                        ImageUrls = r.ReviewImages.Select(img => img.ImageUrl).ToList()
                    }).ToList()
                };

                return summary;
            }
            catch
            {
                // Trả về summary rỗng nếu bảng Reviews chưa được tạo trong DB
                return new RoomReviewsSummaryDto();
            }
        }

        public async Task<(bool Success, string? Error)> AddReviewAsync(int userId, CreateReviewDto dto, string webRootPath)
        {
            if (dto.Rating < 1 || dto.Rating > 5)
                return (false, "Vui lòng chọn số sao đánh giá từ 1 đến 5 sao.");

            var canReview = await CanUserReviewAsync(userId, dto.RoomId);
            if (!canReview)
                return (false, "Bạn chỉ có thể đánh giá sau khi đã hoàn tất lưu trú (Check-out) tại phòng này.");

            var roomExists = await _context.Rooms.AnyAsync(r => r.RoomId == dto.RoomId);
            if (!roomExists)
                return (false, "Phòng không tồn tại.");

            var review = new Review
            {
                RoomId = dto.RoomId,
                UserId = userId,
                Rating = dto.Rating,
                Comment = dto.Comment?.Trim(),
                CreatedAt = DateTime.Now
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            if (dto.Images != null && dto.Images.Count > 0)
            {
                var uploadDir = Path.Combine(webRootPath, "uploads", "reviews");
                if (!Directory.Exists(uploadDir))
                {
                    Directory.CreateDirectory(uploadDir);
                }

                foreach (var file in dto.Images)
                {
                    if (file.Length > 0)
                    {
                        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
                        if (!allowedExtensions.Contains(extension))
                            continue;

                        var fileName = $"{Guid.NewGuid()}{extension}";
                        var filePath = Path.Combine(uploadDir, fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        var reviewImage = new ReviewImage
                        {
                            ReviewId = review.ReviewId,
                            ImageUrl = $"/uploads/reviews/{fileName}",
                            CreatedAt = DateTime.Now
                        };
                        _context.ReviewImages.Add(reviewImage);
                    }
                }
                await _context.SaveChangesAsync();
            }

            return (true, null);
        }

        public async Task<bool> CanUserReviewAsync(int userId, int roomId)
        {
            return await _context.BookingRooms
                .AnyAsync(br => br.RoomId == roomId 
                             && br.Booking.CustomerId == userId 
                             && br.Booking.Status == "CheckedOut");
        }

        public async Task<(bool Success, string? Error)> ReplyReviewAsync(int staffUserId, int reviewId, string response)
        {
            if (string.IsNullOrWhiteSpace(response))
                return (false, "Nội dung phản hồi không được để trống.");

            var review = await _context.Reviews.FindAsync(reviewId);
            if (review == null)
                return (false, "Không tìm thấy bài đánh giá.");

            review.StaffResponse = response.Trim();
            review.RespondedByUserId = staffUserId;
            review.RespondedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<List<ReviewItemDto>> GetAllReviewsAsync()
        {
            try
            {
                var reviews = await _context.Reviews
                    .Include(r => r.Room)
                    .Include(r => r.User)
                    .Include(r => r.RespondedByUser)
                    .Include(r => r.ReviewImages)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();

                return reviews.Select(r => new ReviewItemDto
                {
                    ReviewId = r.ReviewId,
                    RoomId = r.RoomId,
                    RoomNumber = r.Room?.RoomNumber ?? "",
                    UserId = r.UserId,
                    UserName = r.User.FullName ?? "Khách hàng",
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt,
                    StaffResponse = r.StaffResponse,
                    StaffResponderName = r.RespondedByUser?.FullName ?? (r.RespondedByUserId.HasValue ? "Lễ tân khách sạn" : null),
                    RespondedAt = r.RespondedAt,
                    ImageUrls = r.ReviewImages.Select(img => img.ImageUrl).ToList()
                }).ToList();
            }
            catch
            {
                return new List<ReviewItemDto>();
            }
        }

        public async Task<(bool Success, string? Error)> DeleteReviewAsync(int reviewId)
        {
            var review = await _context.Reviews
                .Include(r => r.ReviewImages)
                .FirstOrDefaultAsync(r => r.ReviewId == reviewId);

            if (review == null)
                return (false, "Không tìm thấy bài đánh giá.");

            if (review.ReviewImages != null && review.ReviewImages.Count > 0)
                _context.ReviewImages.RemoveRange(review.ReviewImages);

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();
            return (true, null);
        }
    }
}
