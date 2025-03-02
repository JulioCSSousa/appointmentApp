namespace appointmentApp.Services.Utilities
{
    public static class DayOfWeekHelper
    {
        private static readonly Dictionary<DayOfWeek, string> DaysInPortuguese = new()
    {
        { DayOfWeek.Sunday, "Domingo" },
        { DayOfWeek.Monday, "Segunda-feira" },
        { DayOfWeek.Tuesday, "Terça-feira" },
        { DayOfWeek.Wednesday, "Quarta-feira" },
        { DayOfWeek.Thursday, "Quinta-feira" },
        { DayOfWeek.Friday, "Sexta-feira" },
        { DayOfWeek.Saturday, "Sábado" }
    };

        public static string ToPortuguese(DayOfWeek day)
        {
            return DaysInPortuguese[day];
        }
    }

}
