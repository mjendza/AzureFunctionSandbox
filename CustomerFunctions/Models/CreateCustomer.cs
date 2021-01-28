using FluentValidation;

namespace CustomerFunctions.Models
{
    public class CreateCustomer
    {
        public string Name { get; set; }
    }
    public class CustomerValidator: AbstractValidator<CreateCustomer> {
        public CustomerValidator() {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Name can't be empty.");
        }
    }
}