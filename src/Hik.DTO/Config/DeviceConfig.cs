using FluentValidation;

namespace Hik.DTO.Config
{
    public class DeviceConfig
    {
        public string IpAddress { get; set; }

        public int PortNumber { get; set; }

        public string UserName { get; set; }

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
