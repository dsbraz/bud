using Bud.Client.Services;
using Bud.Client.Shared;
using Bud.Client.Shared.Missions;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bud.Client.Tests.Shared.Missions;

public sealed class MissionWizardModalTests : TestContext
{
    [Fact]
    public void AddMetricFromForm_ShouldAppendMetricToList()
    {
        var toastService = new ToastService();
        Services.AddSingleton(toastService);

        var cut = RenderComponent<MissionWizardModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OrganizationName, "Org")
            .Add(p => p.GetScopeOptions, _ => Enumerable.Empty<ScopeOption>()));

        var instance = cut.Instance;
        var model = GetField<MetricFormFields.MetricFormModel>(instance, "newMetricModel");
        model.Name = "Métrica nova";
        model.TypeValue = "Qualitative";
        model.TargetText = "Texto alvo";

        InvokePrivateVoid(instance, "AddMetricFromForm");

        var metrics = GetField<List<TempMetric>>(instance, "tempMetrics");
        metrics.Should().ContainSingle();
        metrics[0].Name.Should().Be("Métrica nova");
    }

    [Fact]
    public void RemoveTempObjective_ShouldRemoveRelatedMetrics()
    {
        var toastService = new ToastService();
        Services.AddSingleton(toastService);

        var cut = RenderComponent<MissionWizardModal>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OrganizationName, "Org")
            .Add(p => p.GetScopeOptions, _ => Enumerable.Empty<ScopeOption>())
            .Add(p => p.InitialModel, new MissionWizardModel
            {
                Objectives =
                [
                    new TempObjective("obj-1", "Objetivo 1", null)
                ],
                Metrics =
                [
                    new TempMetric(null, "Métrica 1", "Qualitative", "Detalhe", TargetText: "x", ObjectiveTempId: "obj-1")
                ]
            }));

        var instance = cut.Instance;
        InvokePrivateVoid(instance, "RemoveTempObjective", 0);

        var objectives = GetField<List<TempObjective>>(instance, "tempObjectives");
        var metrics = GetField<List<TempMetric>>(instance, "tempMetrics");

        objectives.Should().BeEmpty();
        metrics.Should().BeEmpty();
    }

    private static T GetField<T>(object instance, string name)
        => (T)instance.GetType().GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(instance)!;

    private static void InvokePrivateVoid(object instance, string methodName, params object[] args)
    {
        var method = instance.GetType().GetMethod(methodName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        method.Invoke(instance, args);
    }
}
