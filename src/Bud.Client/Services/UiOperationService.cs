namespace Bud.Client.Services;

public sealed class UiOperationService(ToastService toastService)
{
    public async Task RunAsync(
        Func<Task> operation,
        string errorTitle,
        string errorMessage = "Não foi possível concluir a operação. Tente novamente.")
    {
        try
        {
            await operation();
        }
        catch (HttpRequestException ex)
        {
            Console.Error.WriteLine($"Erro HTTP ({errorTitle}): {ex.Message}");
            var displayMessage = !string.IsNullOrWhiteSpace(ex.Message) ? ex.Message : errorMessage;
            toastService.ShowError(errorTitle, displayMessage);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Erro inesperado ({errorTitle}): {ex.Message}");
            toastService.ShowError(errorTitle, errorMessage);
        }
    }
}
