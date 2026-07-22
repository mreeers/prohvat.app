using FluentValidation;
using ProhvatApp.Application.Vehicles.Commands.AddVehicleLog;

namespace ProhvatApp.Application.Vehicles.Validators;

public class AddVehicleLogCommandValidator : AbstractValidator<AddVehicleLogCommand>
{
    public AddVehicleLogCommandValidator()
    {
        RuleFor(v => v.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(100).WithMessage("Title must not exceed 100 characters.");

        RuleFor(v => v.Content)
            .NotEmpty().WithMessage("Content is required.")
            .MaximumLength(2000).WithMessage("Content must not exceed 2000 characters.");

        RuleFor(v => v.MetricsValue)
            .GreaterThanOrEqualTo(0).WithMessage("Metrics value must be greater than or equal to 0.");
    }
}
