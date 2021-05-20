using System.Collections.Generic;
using System.Net;
using FluentValidation;
using FluentValidation.Results;
using MultiChatLibrary.Models;

namespace MultiChatLibrary.Validators
{
    public class SettingsValidator : AbstractValidator<SettingsModel>
    {
        public SettingsValidator()
        {
            // BufferSize
            RuleFor(s => s.BufferSize).NotEmpty();
            RuleFor(s => s.BufferSize).GreaterThanOrEqualTo(1);

            // IPAdress
            RuleFor(s => s.IPAddress).NotEmpty();
            RuleFor(s => s.IPAddress).Must(ip => !(ip is null) && IsLocalIpAddress(ip.ToString())).WithMessage("IP address is not local.");

            // Name
            RuleFor(s => s.Name).NotEmpty();

            // Port
            RuleFor(s => s.Port).NotEmpty();
            RuleFor(s => s.Port).GreaterThan(0);
            RuleFor(s => s.Port).LessThanOrEqualTo(65535);
        }

        /// <summary>
        /// Checks if provided host is a local IP Address.
        /// </summary>
        /// <param name="host"></param>
        /// <returns>Returns true if IP Address is local</returns>
        public static bool IsLocalIpAddress(string host)
        {
            try
            {
                IPAddress[] hostIPs = Dns.GetHostAddresses(host);
                IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());

                foreach (IPAddress hostIP in hostIPs)
                {
                    // If IP is a loopback host
                    if (IPAddress.IsLoopback(hostIP)) return true;
                    foreach (IPAddress localIP in localIPs)
                    {
                        if (hostIP.Equals(localIP)) return true;
                    }
                }
            }
            catch { }
            return false;
        }

        /// <summary>
        /// Validates the settings using the Fluentvalidation NuGet package.
        /// </summary>
        /// <param name="settings"></param>
        /// <returns>A list of errors or nothing.</returns>
        public List<string> validateSettings(SettingsModel settings)
        {
            SettingsValidator validator = new SettingsValidator();
            ValidationResult results = validator.Validate(settings);

            // If results are not according to rules.
            if (!results.IsValid)
            {
                List<string> errors = new List<string>();
                foreach (ValidationFailure error in results.Errors)
                {
                    errors.Add(error.ErrorMessage.ToString());
                }
                return errors;
            }
            else
            {
                return null;
            }
        }
    }
}