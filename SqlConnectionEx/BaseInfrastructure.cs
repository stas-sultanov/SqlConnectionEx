using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SqlConnectionEx
{
	abstract class BaseInfrastructure
	{
		const string DependencyTypeName = "MySQL";

		TelemetryClient telemetry;

		string DependencyTarget;

		MySqlConnection connection;

		protected BaseInfrastructure(IConfiguration configuration, ILogger logger, TelemetryClient telemetry)
		{
			//this.DependencyTarget = String.Join(" | ", connectionStringBuilder.Server, connectionStringBuilder.Database);
		
			this.telemetry = telemetry;
		}

		/// <summary>
		/// Tracks MySQL Dependency into AppInsights.
		/// </summary>
		/// <typeparam name="TResult">Result of Func</typeparam>
		/// <param name="commandText">Text of SQL command</param>
		/// <param name="commandCall">A func that calls a SQL command.</param>
		/// <returns></returns>
		private async Task TrackDependency(Func<MySqlConnection, CancellationToken, Task> commandCall, CancellationToken cancellationToken)
		{
			var success = false;

			string resultCode = null;

			var startTime = DateTime.UtcNow;

			// start a stop watch
			var stopwatch = System.Diagnostics.Stopwatch.StartNew();

			try
			{
				// open connection
				await connection.OpenAsync(cancellationToken);

				// call stored procedure
				await commandCall(connection, cancellationToken);

				// close connection
				await connection.CloseAsync();

				// stop the stop watch
				stopwatch.Stop();

				// set sucess
				success = true;
			}
			catch (DbException ex)
			{
				// stop the stop watch
				stopwatch.Stop();

				// set success
				success = false;

				// set result code
				resultCode = ex.ErrorCode.ToString();

				throw;
			}
			finally
			{
				var telemetryxx = new DependencyTelemetry(DependencyTypeName, DependencyTarget, DependencyTarget, "", startTime, stopwatch.Elapsed, resultCode, success);

				telemetry.TrackDependency(telemetryxx);
			}
		}

		/// <summary>Initiates an asynchronous operation to execute the stored procedure which returns nothing.</summary>
		/// <param name="connection">Connection to the SQL database.</param>
		/// <param name="name">Name of stored procedure to execute.</param>
		/// <param name="timeout">Time in seconds to wait for the stored proceduer to execute.</param>
		/// <param name="arguments">Collection of the arguments of the stored procedure.</param>
		/// <param name="cancellationToken">The cancellation instruction.</param>
		/// <returns>A <see cref="Task" /> object that represents the asynchronous operation.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public async Task ExecuteStoredProcedureWithNoResultAsync(String name, Int32 timeout, MySqlParameter[] arguments, CancellationToken cancellationToken)
		{
			Task local (MySqlConnection con, )
			{
			}

			this.TrackDependency(SqlConnectionEx,)
		}

	}
}
