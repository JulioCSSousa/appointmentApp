namespace appointmentApp.Models.Entities
{
    public class WorkScheduleModel
    {
        public int Id { get; private set; }
        public Guid? StoreId { get; private set; }
        public StoreModel? Store { get; private set; }
        public Guid? WorkerId { get; private set; } = Guid.Empty;
        public DateTime Day { get; private set; }
        public bool IsSelected { get; private set; }
        public TimeSpan Start { get; private set; }
        public TimeSpan End { get; private set; }
        public List<AppointmentModel> Appointments { get; set; } = new List<AppointmentModel>();

        private WorkScheduleModel() { }
        public WorkScheduleModel(DateTime day, bool isSelected, TimeSpan start, TimeSpan end, Guid? workerId)
        {
            Day = day;
            IsSelected = isSelected;
            Start = start;
            End = end;
            WorkerId = workerId;
        }

        public void SetStore(Guid storeId)
        {
            StoreId = storeId;
        }

        public void SetWorkerId(Guid? id)
        {
            WorkerId = id;
        }
        public void SetDay(DateTime day)
        {
            Day = day;
        }
        public void SetIsSelected(bool isSelected)
        {
            IsSelected = isSelected;
        }
        public void SetStart(TimeSpan start)
        {
            Start = start;
        }
        public void SetEnd(TimeSpan end)
        {
            End = end;
        }

        public void SetAppointment(AppointmentModel appointment)
        {
            Appointments.Add(appointment);
        }
    }

}
