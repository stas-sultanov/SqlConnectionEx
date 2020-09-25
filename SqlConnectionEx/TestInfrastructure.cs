using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using SSO;

namespace SqlConnectionEx
{
	public sealed class TestInfrastructure : BaseInfrastructure
	{
		public TestInfrastructure(IConfiguration configuration, TelemetryClient telemetry) : base (configuration, telemetry)
		{

		}

		public async Task TestMethodAsync(CancellationToken cancellationToken)
		{
			// TODO: pay attention at local function
			static Tuple<int, float> readRecord(DbDataReader reader)
			{
				return Tuple.Create(reader.GetInt32(0), reader.GetFloat(1));
			}

			// execute stored procedure
			// create account
			var @params = new[]
			{
				new MySqlParameter("@param1", 1),
				new MySqlParameter("@param2", 2)
			};

			var result = await ExecuteStoredProcedureWithSetResultAsync("[Scheme].[SPName]", @params, readRecord, cancellationToken);

			// TODO: do something with result
		}
	}
}
