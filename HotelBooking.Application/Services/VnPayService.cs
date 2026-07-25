using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using HotelBooking.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace HotelBooking.Application.Services
{
    public class VnPayService : IVnPayService
    {
        private readonly string _tmnCode;
        private readonly string _hashSecret;
        private readonly string _baseUrl;
        private readonly string _hashType;

        public VnPayService(IConfiguration configuration)
        {
            _tmnCode = configuration["VnPay:TmnCode"]?.Trim() ?? "ULKKS7KL";
            _hashSecret = configuration["VnPay:HashSecret"]?.Trim() ?? "FxPAhE7qjs6cs6MCR8cjoY22Pu7iCES/uotRIqSoOIqwY1nqcnEgGTI5GvthnoS/";
            _baseUrl = configuration["VnPay:Url"]?.Trim() ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
            _hashType = configuration["VnPay:HashType"]?.ToUpperInvariant().Trim() ?? "SHA512";
        }

        public string CreatePaymentUrl(int bookingId, decimal amount, string orderInfo, string returnUrl, string transactionRef, string ipAddress = "127.0.0.1")
        {
            if (string.IsNullOrWhiteSpace(ipAddress) || ipAddress == "::1")
            {
                ipAddress = "127.0.0.1";
            }

            var vnpayData = new Dictionary<string, string?>
            {
                ["vnp_Version"] = "2.1.0",
                ["vnp_Command"] = "pay",
                ["vnp_TmnCode"] = _tmnCode,
                ["vnp_Amount"] = ((int)(amount * 100)).ToString(),
                ["vnp_CreateDate"] = DateTime.UtcNow.AddHours(7).ToString("yyyyMMddHHmmss"),
                ["vnp_CurrCode"] = "VND",
                ["vnp_ExpireDate"] = DateTime.UtcNow.AddHours(7).AddMinutes(15).ToString("yyyyMMddHHmmss"),
                ["vnp_IpAddr"] = ipAddress,
                ["vnp_Locale"] = "vn",
                ["vnp_OrderInfo"] = orderInfo,
                ["vnp_OrderType"] = "other",
                ["vnp_ReturnUrl"] = returnUrl,
                ["vnp_TxnRef"] = transactionRef,
                ["vnp_SecureHashType"] = _hashType,
                ["vnp_BankCode"] = ""
            };

            var sorted = vnpayData.Where(kvp => !string.IsNullOrEmpty(kvp.Value))
                .OrderBy(kvp => kvp.Key, StringComparer.Ordinal)
                .ToList();

            var queryString = BuildQueryString(sorted);
            var signData = BuildHashData(sorted.Where(kvp => kvp.Key != "vnp_SecureHashType"));
            var secureHash = ComputeHash(_hashSecret, signData, _hashType);

            var normalizedOrderInfo = orderInfo?.Replace("+", "%20") ?? string.Empty;
            var normalizedReturnUrl = returnUrl?.Replace("+", "%20") ?? string.Empty;
            Console.WriteLine($"VNPAY orderInfo normalized: {normalizedOrderInfo}");
            Console.WriteLine($"VNPAY returnUrl normalized: {normalizedReturnUrl}");

            Console.WriteLine("VNPAY signData: " + signData);
            Console.WriteLine("VNPAY secureHash: " + secureHash);
            Console.WriteLine("VNPAY URL: " + $"{_baseUrl}?{queryString}&vnp_SecureHash={secureHash}");

            var paymentUrl = $"{_baseUrl}?{queryString}&vnp_SecureHash={secureHash}";
            return paymentUrl;
        }

        public bool ValidateSignature(IQueryCollection query, out string? receivedHash)
        {
            receivedHash = query["vnp_SecureHash"].ToString();
            var hashType = query["vnp_SecureHashType"].ToString();
            if (string.IsNullOrWhiteSpace(hashType))
            {
                hashType = _hashType;
            }
            else
            {
                hashType = hashType.ToUpperInvariant().Trim();
            }

            var queryParams = query
                .Where(kvp => kvp.Key != "vnp_SecureHash" && kvp.Key != "vnp_SecureHashType")
                .OrderBy(kvp => kvp.Key, StringComparer.Ordinal)
                .ToList();

            var signData = BuildHashData(queryParams.Select(kvp => new KeyValuePair<string, string?>(kvp.Key, kvp.Value.ToString())));
            var expectedHash = ComputeHash(_hashSecret, signData, hashType);
            var isValid = string.Equals(expectedHash, receivedHash, StringComparison.OrdinalIgnoreCase);

            Console.WriteLine("VNPAY callback query: " + string.Join("&", query.Select(kvp => $"{kvp.Key}={kvp.Value}")));
            Console.WriteLine("VNPAY callback signData: " + signData);
            Console.WriteLine("VNPAY callback hashType: " + hashType);
            Console.WriteLine("VNPAY callback expectedHash: " + expectedHash);
            Console.WriteLine("VNPAY callback receivedHash: " + receivedHash);
            Console.WriteLine("VNPAY callback isValid: " + isValid);

            if (isValid)
            {
                return true;
            }

            if (hashType == "SHA512")
            {
                var alternativeHash = ComputeHash(_hashSecret, signData, "SHA256");
                Console.WriteLine("VNPAY callback alternative SHA256: " + alternativeHash);
                if (string.Equals(alternativeHash, receivedHash, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("VNPAY callback verified with alternative SHA256");
                    return true;
                }
            }
            else if (hashType == "SHA256")
            {
                var alternativeHash = ComputeHash(_hashSecret, signData, "SHA512");
                Console.WriteLine("VNPAY callback alternative SHA512: " + alternativeHash);
                if (string.Equals(alternativeHash, receivedHash, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("VNPAY callback verified with alternative SHA512");
                    return true;
                }
            }

            return false;
        }

        private static string BuildQueryString(IEnumerable<KeyValuePair<string, string?>> data)
        {
            return string.Join("&", data
                .Where(kvp => !string.IsNullOrEmpty(kvp.Value))
                .OrderBy(kvp => kvp.Key, StringComparer.Ordinal)
                .Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value!)}"));
        }

        private static string BuildHashData(IEnumerable<KeyValuePair<string, string?>> data)
        {
            return string.Join("&", data
                .Where(kvp => !string.IsNullOrEmpty(kvp.Value))
                .OrderBy(kvp => kvp.Key, StringComparer.Ordinal)
                .Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value!)}"));
        }

        private static IEnumerable<KeyValuePair<string, string?>> NormalizeForHash(IEnumerable<KeyValuePair<string, string?>> data)
        {
            return data
                .Where(kvp => kvp.Value != null)
                .Select(kvp => new KeyValuePair<string, string?>(kvp.Key, kvp.Value?.Replace("+", "%20")));
        }

        private string ComputeHash(string key, string inputData, string hashType)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var messageBytes = Encoding.UTF8.GetBytes(inputData);

            byte[] hash;
            switch (hashType)
            {
                case "SHA256":
                    using (var hmac = new HMACSHA256(keyBytes))
                    {
                        hash = hmac.ComputeHash(messageBytes);
                    }
                    break;
                case "SHA512":
                    using (var hmac = new HMACSHA512(keyBytes))
                    {
                        hash = hmac.ComputeHash(messageBytes);
                    }
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported VnPay hash type: {hashType}");
            }

            return BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant();
        }
    }
}
