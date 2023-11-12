using System.ComponentModel.DataAnnotations;
using FluentValidation;

namespace Hik.DTO.Config
{
    public class DeviceConfig
    {
        [Display(Name = "IP Address")]
        public string IpAddress { get; set; }

        [Display(Name = "Port number")]
        public int PortNumber { get; set; }

        [Display(Name = "User name")]
        public string UserName { get; set; }

        [Display(Name = "Password")]
        public string Password { get; set; }
    }

    public class DeviceConfigValidator : AbstractValidator<DeviceConfig>
    {
        public DeviceConfigValidator()
        {
            RuleFor(customer => customer.UserName).NotEmpty();
            RuleFor(customer => customer.Password).NotEmpty();
            RuleFor(customer => customer.PortNumber).GreaterThan(0);
            RuleFor(customer => customer.IpAddress).NotEmpty();
        }
    }
}
