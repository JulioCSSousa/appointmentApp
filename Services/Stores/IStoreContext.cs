using appointmentApp.Models.Entities;

namespace AppointmentAppMvc.Services.Stores;

public interface IStoreContext
{
    Task<StoreModel?> GetCurrentStoreAsync();
}
