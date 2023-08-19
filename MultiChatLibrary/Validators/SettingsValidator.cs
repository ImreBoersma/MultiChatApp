using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using FluentValidation;
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

            // IPAddress
            RuleFor(s => s.IpAddress).NotEmpty();
            RuleFor(s => s.IpAddress).Must(ip => !(ip is null) && IsLocalIpAddress(ip.ToString()))
                .WithMessage("IP address is not local.");

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
        private static bool IsLocalIpAddress(string host)
        {
            try
            {
                var hostIPs = Dns.GetHostAddresses(host);
                var localIPs = Dns.GetHostAddresses(Dns.GetHostName());

                foreach (var hostIp in hostIPs)
                {
                    // If IP is a loopback host
                    if (IPAddress.IsLoopback(hostIp)) return true;
                    if (localIPs.Contains(hostIp))
                    {
                        return true;
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }

            return false;
        }

        /// <summary>
        /// Validates the settings using the Fluentvalidation NuGet package.
        /// </summary>
        /// <param name="settings"></param>
        /// <returns>A list of errors or nothing.</returns>
        public static List<string> ValidateSettings(SettingsModel settings)
        {
            var validator = new SettingsValidator();
            var results = validator.Validate(settings);

            // If results are not according to rules.
            return !results.IsValid
                ? results.Errors.Select(error => error.ErrorMessage.ToString()).ToList()
                : new List<string>();
        }
    }
}