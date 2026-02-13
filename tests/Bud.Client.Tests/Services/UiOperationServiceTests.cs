using Bud.Client.Services;
using FluentAssertions;
using Xunit;

namespace Bud.Client.Tests.Services;

public sealed class UiOperationServiceTests
{
    [Fact]
    public async Task RunAsync_WhenOperationSucceeds_ShouldNotShowErrorToast()
    {
        var toastService = new ToastService();
        var sut = new UiOperationService(toastService);
        ToastMessage? capturedToast = null;
        toastService.OnToastAdded += toast => capturedToast = toast;

        await sut.RunAsync(
            operation: () => Task.CompletedTask,
            errorTitle: "Erro ao criar equipe",
            errorMessage: "Não foi possível criar a equipe.");

        capturedToast.Should().BeNull();
    }

    [Fact]
    public async Task RunAsync_WhenOperationThrowsHttpRequestException_ShouldShowApiErrorMessage()
    {
        var toastService = new ToastService();
        var sut = new UiOperationService(toastService);
        ToastMessage? capturedToast = null;
        toastService.OnToastAdded += toast => capturedToast = toast;

        await sut.RunAsync(
            operation: () => throw new HttpRequestException("Apenas o proprietário pode excluir o item."),
            errorTitle: "Erro ao excluir",
            errorMessage: "Não foi possível excluir o item.");

        capturedToast.Should().NotBeNull();
        capturedToast!.Title.Should().Be("Erro ao excluir");
        capturedToast.Message.Should().Be("Apenas o proprietário pode excluir o item.");
        capturedToast.Type.Should().Be(ToastType.Error);
    }

    [Fact]
    public async Task RunAsync_WhenHttpRequestExceptionHasEmptyMessage_ShouldShowFallbackMessage()
    {
        var toastService = new ToastService();
        var sut = new UiOperationService(toastService);
        ToastMessage? capturedToast = null;
        toastService.OnToastAdded += toast => capturedToast = toast;

        await sut.RunAsync(
            operation: () => throw new HttpRequestException(""),
            errorTitle: "Erro ao criar equipe",
            errorMessage: "Não foi possível criar a equipe. Verifique os dados e tente novamente.");

        capturedToast.Should().NotBeNull();
        capturedToast!.Title.Should().Be("Erro ao criar equipe");
        capturedToast.Message.Should().Be("Não foi possível criar a equipe. Verifique os dados e tente novamente.");
        capturedToast.Type.Should().Be(ToastType.Error);
    }

    [Fact]
    public async Task RunAsync_WhenOperationThrowsUnexpectedException_ShouldShowFallbackMessage()
    {
        var toastService = new ToastService();
        var sut = new UiOperationService(toastService);
        ToastMessage? capturedToast = null;
        toastService.OnToastAdded += toast => capturedToast = toast;

        await sut.RunAsync(
            operation: () => throw new InvalidOperationException("erro inesperado"),
            errorTitle: "Erro ao atualizar",
            errorMessage: "Não foi possível atualizar.");

        capturedToast.Should().NotBeNull();
        capturedToast!.Title.Should().Be("Erro ao atualizar");
        capturedToast.Message.Should().Be("Não foi possível atualizar.");
        capturedToast.Type.Should().Be(ToastType.Error);
    }
}
