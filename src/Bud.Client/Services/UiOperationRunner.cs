namespace Bud.Client.Services;

public static class UiOperationRunner
{
    public static async Task RunAsync(
        Func<Task> operation,
        ToastService toastService,
        string httpErrorTitle,
        string httpErrorMessage,
        string? operationContext = null,
        string unexpectedErrorTitle = "Erro inesperado",
        string unexpectedErrorMessage = "Não foi possível concluir a operação. Tente novamente.")
    {
        try
        {
            await operation();
        }
        catch (HttpRequestException ex)
        {
            UiErrorHandler.HandleHttpError(
                toastService,
                httpErrorTitle,
                httpErrorMessage,
                ex,
                operationContext);
        }
        catch (Exception ex)
        {
            UiErrorHandler.HandleUnexpectedError(
                toastService,
                unexpectedErrorTitle,
                unexpectedErrorMessage,
                ex,
                operationContext);
        }
    }
}
