using Bud.Client.Services;
using FluentAssertions;
using Xunit;

namespace Bud.Client.Tests.Services;

public sealed class UiOperationRunnerTests
{
    [Fact]
    public async Task RunAsync_WhenOperationSucceeds_ShouldNotShowErrorToast()
    {
        var toastService = new ToastService();
        ToastMessage? capturedToast = null;
        toastService.OnToastAdded += toast => capturedToast = toast;

        await UiOperationRunner.RunAsync(
            operation: () => Task.CompletedTask,
            toastService: toastService,
            httpErrorTitle: "Erro ao criar equipe",
            httpErrorMessage: "Não foi possível criar a equipe.");

        capturedToast.Should().BeNull();
    }

    [Fact]
    public async Task RunAsync_WhenOperationThrowsHttpRequestException_ShouldShowConfiguredHttpErrorToast()
    {
        var toastService = new ToastService();
        ToastMessage? capturedToast = null;
        toastService.OnToastAdded += toast => capturedToast = toast;

        await UiOperationRunner.RunAsync(
            operation: () => throw new HttpRequestException("falha técnica"),
            toastService: toastService,
            httpErrorTitle: "Erro ao excluir",
            httpErrorMessage: "Não foi possível excluir o item.");

        capturedToast.Should().NotBeNull();
        capturedToast!.Title.Should().Be("Erro ao excluir");
        capturedToast.Message.Should().Be("Não foi possível excluir o item.");
    }

    [Fact]
    public async Task RunAsync_WhenOperationThrowsUnexpectedException_ShouldShowDefaultUnexpectedToast()
    {
        var toastService = new ToastService();
        ToastMessage? capturedToast = null;
        toastService.OnToastAdded += toast => capturedToast = toast;

        await UiOperationRunner.RunAsync(
            operation: () => throw new InvalidOperationException("erro inesperado"),
            toastService: toastService,
            httpErrorTitle: "Erro ao atualizar",
            httpErrorMessage: "Não foi possível atualizar.");

        capturedToast.Should().NotBeNull();
        capturedToast!.Title.Should().Be("Erro inesperado");
        capturedToast.Message.Should().Be("Não foi possível concluir a operação. Tente novamente.");
    }
}
