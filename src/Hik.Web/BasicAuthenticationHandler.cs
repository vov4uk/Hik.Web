using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace Hik.Web
{
    public class BasicAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private const string Basic = "Basic";
        private readonly IConfiguration configuration;

        public BasicAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock,
            IConfiguration configuration
            ) : base(options, logger, encoder, clock)
        {
            this.configuration = configuration;
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            if (authHeader != null && authHeader.StartsWith(Basic, StringComparison.OrdinalIgnoreCase))
            {
                var token = authHeader.Substring(Basic.Length +1).Trim();
                this.Logger.LogInformation(token);
                var credentialString = Encoding.UTF8.GetString(Convert.FromBase64String(token));

                var allowedUsers = configuration.GetSection("BasicAuthentication:AllowedUsers").Get<string[]>();

                if (allowedUsers.Contains(credentialString))
                {
                    var userName = credentialString.Split(':')[0];
                    var claims = new[] { new Claim("name", userName), new Claim(ClaimTypes.Role, "Admin") };
                    var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims, Basic));
                    return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(claimsPrincipal, Scheme.Name)));
                }

                return NotAuthentificated(credentialString);
            }
            else
            {
                return NotAuthentificated();
            }
        }

        private Task<AuthenticateResult> NotAuthentificated(string credentialsString = null)
        {
            this.Logger.LogError("Invalid login from {request} ({credentials})", Request.Host.Value, credentialsString);
            Response.StatusCode = 401;
            Response.Headers.Add("WWW-Authenticate", "Basic realm=\"hikweb.net\"");
            return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization Header"));
        }
    }
}
