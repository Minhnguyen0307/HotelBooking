namespace HotelBooking.Application.Settings
{
    public class HotelPolicies
    {
        public int StandardGuestsPerRoom { get; set; } = 2;
        public decimal ExtraAdultFeePerNight { get; set; } = 150000;
        public decimal ExtraChildFeePerNight { get; set; } = 80000;
        public double WeekendMarkupPercent { get; set; } = 15.0;
        public double HolidayMarkupPercent { get; set; } = 25.0;
        public List<string> Holidays { get; set; } = new();
    }
}
