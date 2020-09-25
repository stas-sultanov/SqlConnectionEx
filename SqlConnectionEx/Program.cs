using System;
using System.Threading;
using System.Threading.Tasks;
using SqlConnectionEx;

namespace SSO.SqlConnectionEx
{
	class Program
	{
		static async Task Main()
		{
			var x = new TestInfrastructure(null, null);

			await x.TestMethodAsync(CancellationToken.None);
		}

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
