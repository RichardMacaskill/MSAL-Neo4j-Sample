using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;
using System.Net.Http.Headers;
/*
Notes;
    - requires allowing a redirect URI of http://localhost:58587 under 'Mobile and desktop applications'
    - requires forcing AAD application to return v2 oAuth tokens (by changing the application manifest)
    {
	"id": "ed9cc119-d611-4660-aafd-3888e73e3351",
	"acceptMappedClaims": null,
	"accessTokenAcceptedVersion": 2,
	"addIns": [],....

*/
namespace MSALNeo4jSample
{
    internal class Program
    {
        private static PublicClientApplicationOptions? appConfiguration = null;
        private static IConfiguration? configuration;

        // The MSAL Public client app
        private static IPublicClientApplication? application;

        private static async Task Main(string[] args)
        {
         var thistoken  = await BuildApplicationRequiringToken();
        }
    private static async Task<AuthenticationHeaderValue> BuildApplicationRequiringToken()
        {
               // Using appsettings.json for our configuration settings
            var builder = new ConfigurationBuilder()
                .SetBasePath(System.IO.Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

            configuration = builder.Build();

            appConfiguration = configuration
                .Get<PublicClientApplicationOptions>();

            // We intend to obtain a token for Graph for the following scopes (permissions)
            string[] scopes = new[] { string.Concat(appConfiguration.ClientId , "/.default") };

            
            var mytoken = new AuthenticationHeaderValue("bearer", await SignInUserAndGetTokenUsingMSAL(appConfiguration, scopes));

            Console.WriteLine(mytoken.ToString());

            return mytoken;
        }
        private static async Task<string> SignInUserAndGetTokenUsingMSAL(PublicClientApplicationOptions configuration, string[] scopes)
        {
            string authority = string.Concat(configuration.Instance, configuration.TenantId);

            // Initialize the MSAL library by building a public client application
            application = PublicClientApplicationBuilder.Create(configuration.ClientId)
                                                    .WithAuthority(authority)
                                                    .WithDefaultRedirectUri()
                                                    .Build();


            AuthenticationResult result;
            try
            {
                var accounts = await application.GetAccountsAsync();
                result = await application.AcquireTokenSilent(scopes, accounts.FirstOrDefault())
                 .ExecuteAsync();
            }
            catch (MsalUiRequiredException ex)
            {
                result = await application.AcquireTokenInteractive(scopes)
                 .WithClaims(ex.Claims)
                 .ExecuteAsync();
            }

            return result.AccessToken;
        }
 
        /// <summary>
        /// Sign in user to AAD and obtain a token for Neo4j
        /// </summary>
        /// <returns></returns>
    }
}
