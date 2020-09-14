using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Data.SqlClient;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SSO.SqlConnectionEx
{
	class Program
	{
		static async Task Main(CancellationToken cancellationToken)
		{
			var connection = await GetSqlConnectionAsync(cancellationToken);

			// open connection
			await connection.OpenAsync(cancellationToken);

			// call sample procedure
			await TestStoredProcedure(connection, cancellationToken);

			// close connection
			await connection.CloseAsync();
		}

		private static async Task TestStoredProcedure(SqlConnection connection, CancellationToken cancellationToken)
		{
			// TODO: pay attention at local function
			static Tuple<int, float> read (SqlDataReader reader)
			{
				return Tuple.Create(reader.GetInt32(0), reader.GetFloat(1) );
			}

			// execute stored procedure
			// create account
			var @params = new[]
			{
				new SqlParameter("@param1", 1),
				new SqlParameter("@param2", 2)
			};

			var result = await connection.ExecuteStoredProcedureWithSetResultAsync("[Scheme].[SPName]", 10, @params, read, cancellationToken);

			// TODO: do something with result
		}

		private static async Task<SqlConnection> GetSqlConnectionAsync(CancellationToken cancellationToken)
		{
			// get active directory tennant id
			var activeDirectoryTenantId = Environment.GetEnvironmentVariable(@"ActiveDirectory:TenantId");

			// create token provider
			var azureServiceTokenProvider = new AzureServiceTokenProvider();

			// get connection string
			var sqlDatabaseConnectionString = Environment.GetEnvironmentVariable(@"Database:ConnectionString");

			// get access token
			var sqlDatabaseAccessToken = await azureServiceTokenProvider.GetAccessTokenAsync(@"https://database.windows.net/", activeDirectoryTenantId, cancellationToken);

			// create new connection
			var result = new SqlConnection(sqlDatabaseConnectionString)
			{
				// set access token
				AccessToken = sqlDatabaseAccessToken
			};

			return result;
		}
	}
}
