using Bud.Client.Services;
using FluentAssertions;
using Xunit;

namespace Bud.Client.Tests.Services;

public sealed class UiErrorHandlerTests
{
    [Fact]
    public void HandleHttpError_ShouldPublishFallbackMessage()
    {
        var toastService = new ToastService();
        ToastMessage? capturedToast = null;
        toastService.OnToastAdded += toast => capturedToast = toast;

        UiErrorHandler.HandleHttpError(
            toastService,
            "Erro ao criar equipe",
            "Não foi possível criar a equipe. Verifique os dados e tente novamente.",
            new HttpRequestException("Detalhe técnico"));

        capturedToast.Should().NotBeNull();
        capturedToast!.Title.Should().Be("Erro ao criar equipe");
        capturedToast.Message.Should().Be("Não foi possível criar a equipe. Verifique os dados e tente novamente.");
        capturedToast.Type.Should().Be(ToastType.Error);
    }

    [Fact]
    public void HandleUnexpectedError_ShouldPublishFallbackMessage()
    {
        var toastService = new ToastService();
        ToastMessage? capturedToast = null;
        toastService.OnToastAdded += toast => capturedToast = toast;

        UiErrorHandler.HandleUnexpectedError(
            toastService,
            "Erro inesperado",
            "Não foi possível concluir a operação. Tente novamente.",
            new InvalidOperationException("Falha inesperada"));

        capturedToast.Should().NotBeNull();
        capturedToast!.Title.Should().Be("Erro inesperado");
        capturedToast.Message.Should().Be("Não foi possível concluir a operação. Tente novamente.");
        capturedToast.Type.Should().Be(ToastType.Error);
    }
}
