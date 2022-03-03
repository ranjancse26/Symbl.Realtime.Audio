using Microsoft.Extensions.Configuration;
using SymblAISharp.Authentication;

namespace Symbl.Realtime.Audio
{
    public class SymblAuth
    {
        protected IConfigurationRoot configurationRoot;

        public SymblAuth()
        {
            configurationRoot = new ConfigurationBuilder()
                .AddJsonFile("appSettings.json").Build();
        }

        public AuthResponse GetAuthToken()
        {
            string appId = configurationRoot["appId"];
            string appSecret = configurationRoot["appSecret"];

            AuthenticationApi authentication = new AuthenticationApi();

            var authResponse = authentication.GetAuthToken(
                new AuthRequest
                {
                    type = "application",
                    appId = appId,
                    appSecret = appSecret
                });

            return authResponse;
        }
    }
}
