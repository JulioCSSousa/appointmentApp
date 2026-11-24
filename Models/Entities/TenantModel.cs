using System.ComponentModel.DataAnnotations;

namespace appointmentApp.Models.Entities;

public class TenantModel
{
    [Key]
    public Guid Id { get; private set; }

    [Required]
    [MaxLength(120)]
    public string Name { get; private set; } = string.Empty;

    [Required]
    [MaxLength(80)]
    public string Slug { get; private set; } = string.Empty;

    public StoreModel? Store { get; private set; }

    private TenantModel() { }

    public TenantModel(string name, string slug)
    {
        Id = Guid.NewGuid();
        Name = name;
        Slug = slug;
    }

    public void SetStore(StoreModel store)
    {
        Store = store;
    }

    public void SetName(string name)
    {
        Name = name.Trim();
    }
}
