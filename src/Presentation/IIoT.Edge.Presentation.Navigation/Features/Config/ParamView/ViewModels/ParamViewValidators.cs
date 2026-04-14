using IIoT.Edge.Application.Common.Crud;
using IIoT.Edge.Application.Features.Config.ParamView.Models;

namespace IIoT.Edge.Presentation.Navigation.Features.Config.ParamView;

internal sealed class GeneralParamValidator : IEditorValidator<GeneralParamVm>
{
    public Task<IReadOnlyCollection<ValidationIssue>> ValidateAsync(
        GeneralParamVm model,
        CancellationToken cancellationToken = default)
    {
        var issues = new List<ValidationIssue>();

        if (string.IsNullOrWhiteSpace(model.Name))
        {
            issues.Add(new ValidationIssue("General parameter name is required.", nameof(model.Name)));
        }

        return Task.FromResult<IReadOnlyCollection<ValidationIssue>>(issues);
    }
}

internal sealed class DeviceParamValidator : IEditorValidator<DeviceParamVm>
{
    public Task<IReadOnlyCollection<ValidationIssue>> ValidateAsync(
        DeviceParamVm model,
        CancellationToken cancellationToken = default)
    {
        var issues = new List<ValidationIssue>();

        if (string.IsNullOrWhiteSpace(model.Name))
        {
            issues.Add(new ValidationIssue("Device parameter name is required.", nameof(model.Name)));
        }

        return Task.FromResult<IReadOnlyCollection<ValidationIssue>>(issues);
    }
}
