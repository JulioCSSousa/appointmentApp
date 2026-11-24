namespace AppointmentAppMvc.Services.Stores;

public sealed class TenantContext : ITenantContext
{
    public Guid? StoreId { get; private set; }

    public bool IsResolved => StoreId.HasValue;

    public void SetStore(Guid storeId)
    {
        StoreId = storeId;
    }

    public void Clear()
    {
        StoreId = null;
    }
}
