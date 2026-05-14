using MySqlConnector;

namespace AppointmentAppMvc.Middlewares
{
    // Crie uma classe para capturar o erro
    public class DatabaseExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public DatabaseExceptionMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                // Verifica se é erro de conexão (MySqlConnector ou EF Core)
                if (IsDatabaseOffline(ex))
                {
                    if (ex.Message.Contains("Unable to connect") || ex.InnerException?.Message.Contains("Unable to connect") == true)
                    {
                        // Evita redirecionar se já estivermos na página de erro (evita loop infinito)
                        if (!context.Request.Path.StartsWithSegments("/Home/OfflineDbError"))
                        {
                            context.Response.Redirect("/Home/OfflineDbError");
                            return; // Importante para parar o pipeline
                        }
                    }
                }
                throw; // Se for outro erro, deixa o ExceptionHandler padrão cuidar
            }
        }

    private bool IsDatabaseOffline(Exception ex)
{
    // Verifica a mensagem da exceção atual e das internas (InnerExceptions)
    var currentEx = ex;
    while (currentEx != null)
    {
        if (currentEx.Message.Contains("Unable to connect") || 
            currentEx.Message.Contains("Connection refused") ||
            currentEx is MySqlConnector.MySqlException)
        {
            return true;
        }
        currentEx = currentEx.InnerException;
    }
    return false;
}
    }
}
