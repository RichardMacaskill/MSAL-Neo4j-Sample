using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;
using System.Net.Http.Headers;
using Neo4j.Driver;
using Neo4j.Driver.Experimental;
using GraphDatabase = Neo4j.Driver.GraphDatabase;
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
        private static IPublicClientApplication? application;
        private static string? Neo4j_URI;

        // Entry point for the console application
        private static async Task Main(string[] args)
        {
            var thistoken = await BuildApplicationRequiringToken();
            await ExecuteNeo4jQueryUsingToken(thistoken);
        }
        private static async Task<String> BuildApplicationRequiringToken()
        {
            // Using appsettings.json for our configuration settings
            var builder = new ConfigurationBuilder()
                .SetBasePath(System.IO.Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

            configuration = builder.Build();

            appConfiguration = configuration
                .Get<PublicClientApplicationOptions>();

            // We intend to obtain a token for Neo4j for the following scopes (permissions)
            string[] scopes = new[] { string.Concat(appConfiguration.ClientId, "/.default") };

            Neo4j_URI = configuration["Neo4jUri"];

            var mytoken = new AuthenticationHeaderValue("bearer", await SignInUserAndGetTokenUsingMSAL(appConfiguration, scopes));

            // Remove the "Bearer " section from the token string
            return mytoken.ToString().Remove(0, 7);

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
        private static async Task ExecuteNeo4jQueryUsingToken(String token)
        {
            // create a connection to the Neo4j server using the token
            var driver = GraphDatabase.Driver(Neo4j_URI, AuthTokens.Bearer(token));

            var records = await driver
                .ExecutableQuery("MATCH(m:Movie) return m.title as moviename;")
                .WithConfig(new(database: "movies"))
                .ExecuteAsync();

            // write out the list of movies to show it worked!
           records.Result.ToList().ForEach(i => Console.WriteLine(i.Values["moviename"]));

        }
        /// <summary>
        /// Sign in user to AAD and obtain a token for Neo4j, then use it to authenticate and execute a query using the driver level query API
        /// </summary>
        /// <returns></returns>
    }
}
