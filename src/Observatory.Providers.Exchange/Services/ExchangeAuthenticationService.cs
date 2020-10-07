using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;
using Splat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Observatory.Providers.Exchange.Services
{
    public class ExchangeAuthenticationService
    {
        private readonly IPublicClientApplication _app;
        //private bool _isCacheRegistered = false;

        public ExchangeAuthenticationService()
        {
            _app = PublicClientApplicationBuilder
                .Create(Config.CLIENT_ID)
                .WithRedirectUri(Config.REDIRECT_URI)
                .Build();
        }

        //private async Task RegisterCacheAsync()
        //{
        //    if (!_isCacheRegistered)
        //    {
        //        _isCacheRegistered = true;
        //        var storageProperties = new StorageCreationPropertiesBuilder(
        //            Config.CACHE_FILE_NAME,
        //            Config.CACHE_DIR,
        //            Config.CLIENT_ID)
        //            .WithLinuxKeyring(
        //                Config.LINUX_KEY_RING_SCHEMA,
        //                Config.LINUX_KEY_RING_COLLECTION,
        //                Config.LINUX_KEY_RING_LABEL,
        //                Config.LINUX_KEY_RING_ATTR_1,
        //                Config.LINUX_KEY_RING_ATTR_2)
        //            .WithMacKeyChain(
        //                Config.KEY_CHAIN_SERVICE_NAME,
        //                Config.KEY_CHAIN_ACCOUNT_NAME)
        //            .Build();
        //        var cacheHelper = await MsalCacheHelper.CreateAsync(storageProperties);
        //        cacheHelper.RegisterCache(_app.UserTokenCache);
        //    }
        //}

        public async Task<AuthenticationResult> AcquireTokenSilentAsync(string emailAddress)
        {
            var account = (await _app.GetAccountsAsync()).FirstOrDefault(a => a.Username == emailAddress);
            return await _app.AcquireTokenSilent(Config.SCOPES, account).ExecuteAsync();
        }

        public async Task<AuthenticationResult> AcquireTokenInteractiveAsync(string emailAddress = null)
        {
            var request = _app.AcquireTokenInteractive(Config.SCOPES);
            var account = (await _app.GetAccountsAsync()).FirstOrDefault(a => a.Username == emailAddress);

            if (account != null)
            {
                request = request.WithAccount(account);
            }
            else if (emailAddress != null)
            {
                request = request.WithLoginHint(emailAddress);
            }

            return await request.ExecuteAsync();
        }

        private static class Config
        {
            public static readonly string[] SCOPES = new[]
            {
                "email",
                "offline_access",
                "User.Read",
                "Mail.ReadWrite",
                "Mail.Send",
                "Calendars.ReadWrite"
            };
            public const string CLIENT_ID = "75943d48-0966-4b93-8bd2-c10acd627f19";
            public const string REDIRECT_URI = "http://localhost";

            public const string CACHE_FILE_NAME = "msalcache.dat";
#if DEBUG
            public const string CACHE_DIR = ".";
#elif RELEASE
            public static readonly string CACHE_DIR = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
#endif

            public const string KEY_CHAIN_SERVICE_NAME = "msal_service";
            public const string KEY_CHAIN_ACCOUNT_NAME = "msal_account";

            public const string LINUX_KEY_RING_SCHEMA = "observatory.tokencache";
            public const string LINUX_KEY_RING_COLLECTION = MsalCacheHelper.LinuxKeyRingDefaultCollection;
            public const string LINUX_KEY_RING_LABEL = "MSAL token cache for Observatory app.";
            public static readonly KeyValuePair<string, string> LINUX_KEY_RING_ATTR_1 = new KeyValuePair<string, string>("Version", "1");
            public static readonly KeyValuePair<string, string> LINUX_KEY_RING_ATTR_2 = new KeyValuePair<string, string>("ProductGroup", "Observatory");
        }
    }
}
