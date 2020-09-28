using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using MySqlConnector;
using SSO;

namespace SqlConnectionEx
{
	public sealed class CustomrInfrastructure : BaseInfrastructure
	{
		public CustomrInfrastructure(DbConfiguration configuration, TelemetryClient telemetry) : base(configuration, telemetry)
		{
		}

		/// <summary>
		/// GetAll fetch and returns queried list of items from database.
		/// </summary>
		/// <param name="customer"></param>
		/// <returns></returns>
		public async Task CustomerGetAll(CancellationToken cancellationToken)
		{
			// create input parameters
			var parameters = new DbParameter[]
			{
				new MySqlParameter("PTotalRecord", MySqlDbType.Int32)
				{
					Direction = System.Data.ParameterDirection.Output
				},
				new MySqlParameter("POffset", 25),
				new MySqlParameter("PPageSize", 50),
				new MySqlParameter("PSortColumn", null),
				new MySqlParameter("PCurrentUserId", 115),
				new MySqlParameter("PSortAscending", false),
				new MySqlParameter("PActive", true),
				new MySqlParameter("PIsAppointment", false),
				new MySqlParameter("PSearchText", null)
			};

			static Customer ReadCustomerRecord(DbDataReader dataReader)
			{
				return new Customer
				{
					CustomerId = (UInt32) dataReader.GetInt32(0),
					CustomerName = dataReader.GetString(1),
					Email = dataReader.GetString(2),
					PhoneNumber = dataReader.GetString(3)
				};
			}

			// call method
			var customers = await ExecuteStoredProcedureWithSetResultAsync<Customer>(@"CustomerGetAll", parameters, ReadCustomerRecord, cancellationToken);

			var totalRecords = parameters[0].Value;
		}

		public class Customer
		{
			#region Propeties
			public uint CustomerId { get; set; }
			public uint AgentId { get; set; }
			public int ReasonCodeId { get; set; }
			public string CustomerName { get; set; }
			public string Email { get; set; }
			public string PhoneNumber { get; set; }

			#endregion
		}
	}
}
