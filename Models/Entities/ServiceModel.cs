using System.ComponentModel.DataAnnotations;

public class ServiceModel{
    [Key]
    public int ServiceId { get; private set; }
    [Required(ErrorMessage = "Este campo é obrigatório")]
    [StringLength(50)]
    public string ServiceName { get; private set; }
    [DataType(DataType.Currency)]
    [Required(ErrorMessage = "Este campo é obrigatório")]
    public double Price { get; private set; }
    [DataType(DataType.Time)]
    [Required(ErrorMessage = "Este campo é obrigatório")]
    public TimeSpan Duration { get; private set; }
    [Required(ErrorMessage = "Este campo é obrigatório")]
    public string Category { get; private set; }

    private ServiceModel() { }
    
    public ServiceModel(string serviceName, double price, TimeSpan duration, string category)
    {
        ServiceName = serviceName;
        Price = price;
        Duration = duration;
        Category = category;
    }

    public void SetServiceName(string serviceName)
    {
        ServiceName = serviceName;
    }
    public void SetPrice(double price)
    {
        Price = price;
    }
    public void SetDuration(TimeSpan duration)
    {
        Duration = duration;
    }
    public void SetCategory(string category)
    {
        Category = category;
    }

}