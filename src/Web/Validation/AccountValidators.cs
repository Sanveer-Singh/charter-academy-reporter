using Charter.Reporter.Web.Controllers;
using FluentValidation;

namespace Charter.Reporter.Web.Validation;

public class LoginVmValidator : AbstractValidator<AccountController.LoginVm>
{
    public LoginVmValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
    }
}

public class RegisterVmValidator : AbstractValidator<AccountController.RegisterVm>
{
    public RegisterVmValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty();
        RuleFor(x => x.LastName).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Organization).NotEmpty();
        RuleFor(x => x.Role).NotEmpty();
        RuleFor(x => x.IdNumber).NotEmpty();
        RuleFor(x => x.Cell).NotEmpty();
        RuleFor(x => x.Address).NotEmpty();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.ConfirmPassword).Equal(x => x.Password);
    }
}


