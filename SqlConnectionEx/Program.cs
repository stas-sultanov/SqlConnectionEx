using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using MySqlConnector;
using SqlConnectionEx;

namespace SSO.SqlConnectionEx
{
	class Program
	{
		static async Task Main()
		{
			var dbConfiguration = new DbConfiguration
			{
				ConnectionTimeout = 10,
				MainConnectionString = "",
				ReadConnectionString = ""
			};

			var telemtryClient = new TelemetryClient(new TelemetryConfiguration(""));

			var x = new CustomrInfrastructure(dbConfiguration, telemtryClient);

			await x.CustomerGetAll(CancellationToken.None);
		}

		//private static async Task Main(string serverName)
		//{
		//	var requestUriString = $"http://{serverName}/metadata/identity/oauth2/token?api-version=2018-02-01&resource=https%3A%2F%2Fossrdbms-aad.database.windows.net&client_id=

		//	var request = (HttpWebRequest)WebRequest.Create(requestUriString);
		//	request.Headers["Metadata"] = "true";
		//	request.Method = "GET";
		//	string accessToken = null;

		//	try
		//	{
		//		// Call managed identities for Azure resources endpoint.
		//		HttpWebResponse response = (HttpWebResponse)request.GetResponse();

		//		// Pipe response Stream to a StreamReader and extract access token.
		//		var streamResponse = new StreamReader(response.GetResponseStream());
		//		string stringResponse = streamResponse.ReadToEnd();
		//		var list = JsonSerializer.Deserialize<Dictionary<string, string>>(stringResponse);
		//		accessToken = list["access_token"];
		//	}
		//	catch (Exception e)
		//	{
		//		Console.Out.WriteLine("{0} \n\n{1}", e.Message, e.InnerException != null ? e.InnerException.Message : "Acquire token failed");
		//		System.Environment.Exit(1);
		//	}

		//	//
		//	// Open a connection to the MySQL server using the access token.
		//	//
		//	var builder = new MySqlConnectionStringBuilder
		//	{
		//		Server = Host,
		//		Database = Database,
		//		UserID = User,
		//		Password = accessToken,
		//		SslMode = MySqlSslMode.Required,
		//	};
		//}

		//private static async Task<SqlConnection> GetSqlConnectionAsync(CancellationToken cancellationToken)
		//{
		//	// get active directory tennant id
		//	var activeDirectoryTenantId = Environment.GetEnvironmentVariable(@"ActiveDirectory:TenantId");

		//	// create token provider
		//	var azureServiceTokenProvider = new AzureServiceTokenProvider();

		//	// get connection string
		//	var sqlDatabaseConnectionString = Environment.GetEnvironmentVariable(@"Database:ConnectionString");

		//	// get access token
		//	var sqlDatabaseAccessToken = await azureServiceTokenProvider.GetAccessTokenAsync(@"https://database.windows.net/", activeDirectoryTenantId, cancellationToken);

		//	// create new connection
		//	var result = new SqlConnection(sqlDatabaseConnectionString)
		//	{
		//		// set access token
		//		AccessToken = sqlDatabaseAccessToken
		//	};

		//	return result;
		//}
	}
}
