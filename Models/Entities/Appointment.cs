
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Appointment{

    [Key]
    public Guid Id { get; set; }
    public string ClientId { get; private set; }
    public int ServiceId { get; private set; }
    public DateTime Day { get; private set; }
    public TimeSpan Hour { get; private set; }

    private Appointment() { }
    public Appointment(Guid id, string clientId, int serviceId, DateTime day)
    {
        Id = id;
        ClientId = clientId;
        ServiceId = serviceId;
        Day = day;
    }

    public void SetClientId(string id)
    {
        ClientId = id;
    }
    public void SetServiceId(int serviceId)
    {
        ServiceId = serviceId;
    }
    public void SetDay(DateTime day)
    {
        Day = day;
    }
    public void SetHour(TimeSpan hour)
    {
        Hour = hour;
    }
}