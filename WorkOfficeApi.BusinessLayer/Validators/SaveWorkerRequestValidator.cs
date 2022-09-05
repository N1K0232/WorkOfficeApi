using FluentValidation;
using WorkOfficeApi.Shared.Requests;

namespace WorkOfficeApi.BusinessLayer.Validators;

internal sealed class SaveWorkerRequestValidator : AbstractValidator<SaveWorkerRequest>
{
    public SaveWorkerRequestValidator()
    {
        RuleFor(w => w.FirstName)
            .NotNull()
            .NotEmpty()
            .MaximumLength(256)
            .WithMessage("you must provide a valid firstname");

        RuleFor(w => w.LastName)
            .NotNull()
            .NotEmpty()
            .MaximumLength(256)
            .WithMessage("you must provide a valid lastname");

        RuleFor(w => w.DateOfBirth)
            .NotNull()
            .WithMessage("you must provide a valid date of birth");

        RuleFor(w => w.City)
            .NotNull()
            .NotEmpty()
            .MaximumLength(50)
            .WithMessage("you must provide a valid city");

        RuleFor(w => w.Country)
            .NotNull()
            .NotEmpty()
            .MaximumLength(50)
            .WithMessage("you must provide a valid country");

        RuleFor(w => w.HomeAddress)
            .NotNull()
            .NotEmpty()
            .MaximumLength(256)
            .WithMessage("you must provide a valid home address");

        RuleFor(w => w.CellphoneNumber)
            .NotNull()
            .NotEmpty()
            .MaximumLength(30)
            .WithMessage("you must provide a valid cellphone number");

        RuleFor(w => w.EmailAddress)
            .NotNull()
            .NotEmpty()
            .MaximumLength(100)
            .WithMessage("you must provide a valid email address");

        RuleFor(w => w.WorkerType)
            .NotNull()
            .WithMessage("you must add the worker type");
    }
}