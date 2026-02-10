namespace Bud.Client.Services;

public static class UiErrorHandler
{
    public static void HandleHttpError(
        ToastService toastService,
        string toastTitle,
        string userMessage,
        HttpRequestException exception,
        string? operationContext = null)
    {
        var context = string.IsNullOrWhiteSpace(operationContext) ? toastTitle : operationContext;
        Console.Error.WriteLine($"Erro HTTP ({context}): {exception.Message}");
        var message = string.IsNullOrWhiteSpace(exception.Message) ? userMessage : exception.Message;
        toastService.ShowError(toastTitle, message);
    }

    public static void HandleUnexpectedError(
        ToastService toastService,
        string toastTitle,
        string userMessage,
        Exception exception,
        string? operationContext = null)
    {
        var context = string.IsNullOrWhiteSpace(operationContext) ? toastTitle : operationContext;
        Console.Error.WriteLine($"Erro inesperado ({context}): {exception.Message}");
        toastService.ShowError(toastTitle, userMessage);
    }
}
