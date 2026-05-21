namespace AppointmentAppMvc.Models.ViewModels.WorkSchedule
{
    public class DayScheduleViewModel
    {
        public int Id { get; set; }
        public Guid? WokerId { get; set; }
        public DateTime Day { get; set; }
        public bool IsSelected { get; set; }
        public TimeSpan Start { get; set; }
        public TimeSpan End { get; set; }

        public static List<TimeSpan> GetSchedules(TimeSpan start, TimeSpan end)
        {
            List<TimeSpan> schedules = new List<TimeSpan>();
            for (int i = start.Hours; i < end.Hours; i++)
            {

                schedules.Add(new TimeSpan(i, 0, 0));
            }
            return schedules;
        }
    }
}
