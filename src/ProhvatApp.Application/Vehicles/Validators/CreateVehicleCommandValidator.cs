using FluentValidation;
using ProhvatApp.Application.Vehicles.Commands.CreateVehicle;

namespace ProhvatApp.Application.Vehicles.Validators;

public class CreateVehicleCommandValidator : AbstractValidator<CreateVehicleCommand>
{
    public CreateVehicleCommandValidator()
    {
        RuleFor(v => v.Brand)
            .NotEmpty().WithMessage("Brand is required.")
            .MaximumLength(50).WithMessage("Brand must not exceed 50 characters.");

        RuleFor(v => v.Model)
            .NotEmpty().WithMessage("Model is required.")
            .MaximumLength(50).WithMessage("Model must not exceed 50 characters.");

        RuleFor(v => v.Year)
            .GreaterThan(1900).WithMessage("Year must be greater than 1900.")
            .LessThanOrEqualTo(DateTime.Now.Year + 1).WithMessage($"Year cannot exceed {DateTime.Now.Year + 1}.");

        RuleFor(v => v.CategoryId)
            .NotEmpty().WithMessage("Category is required.");
            
        RuleFor(v => v.MaintenanceInterval)
            .GreaterThanOrEqualTo(0).WithMessage("Maintenance interval must be greater than or equal to 0.");
    }
}
