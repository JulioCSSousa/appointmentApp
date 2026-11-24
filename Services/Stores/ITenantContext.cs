namespace AppointmentAppMvc.Services.Stores;

public interface ITenantContext
{
    Guid? StoreId { get; }

    bool IsResolved { get; }

    void SetStore(Guid storeId);

    void Clear();
}
