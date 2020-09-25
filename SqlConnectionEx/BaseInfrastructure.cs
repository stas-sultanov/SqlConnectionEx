using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace SSO
{
	public abstract class BaseInfrastructure
	{
		#region Data

		const string DependencyTypeName = "MySQL";

		string DependecyName { get; }

		string ConnectionString { get; }

		TelemetryClient TelemetryClient { get; }

		MySqlConnection Connection { get; }

		int Timeout { get; }

		#endregion

		#region Constructor

		protected BaseInfrastructure(IConfiguration configuration, TelemetryClient telemetry)
		{
			ConnectionString = configuration.GetConnectionString(@"DefaultConnection");

			var connectionStringBuilder = new MySqlConnectionStringBuilder(ConnectionString);

			DependecyName = String.Join(" | ", connectionStringBuilder.Server, connectionStringBuilder.Database);

			TelemetryClient = telemetry;

			Timeout = 30;
		}

		#endregion

		#region Methods

		/// <summary>Executes stored procedure procedure and tracks dependency to AppInsights</summary>
		/// <typeparam name="TResult">Result of Func</typeparam>
		/// <param name="commandText">Text of SQL command</param>
		/// <param name="commandCall">A func that calls a SQL command.</param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private async Task ExecuteStoredProcedureAndTrackDependencyAsync(Func<DbConnection, CancellationToken, Task> commandCall, CancellationToken cancellationToken)
		{
			var success = false;

			string resultCode = null;

			var startTime = DateTime.UtcNow;

			// start a stop watch
			var stopwatch = System.Diagnostics.Stopwatch.StartNew();

			try
			{
				// open connection
				await Connection.OpenAsync(cancellationToken);

				// call stored procedure
				await commandCall(Connection, cancellationToken);

				// close connection
				await Connection.CloseAsync();

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
				var telemetry = new DependencyTelemetry(DependencyTypeName, DependecyName, DependecyName, "", startTime, stopwatch.Elapsed, resultCode, success);

				// track dependency
				TelemetryClient.TrackDependency(telemetry);
			}
		}

		/// <summary>Initiates an asynchronous operation to execute the stored procedure which returns nothing.</summary>
		/// <param name="name">Name of stored procedure to execute.</param>
		/// <param name="arguments">Collection of the arguments of the stored procedure.</param>
		/// <param name="cancellationToken">The cancellation instruction.</param>
		/// <returns>A <see cref="Task" /> object that represents the asynchronous operation.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected async Task ExecuteStoredProcedureWithNoResultAsync(String name, DbParameter[] arguments, CancellationToken cancellationToken)
		{
			var success = false;

			string resultCode = null;

			var startTime = DateTime.UtcNow;

			// start a stop watch
			var stopwatch = System.Diagnostics.Stopwatch.StartNew();

			try
			{
				// open connection
				await Connection.OpenAsync(cancellationToken);

				// call stored procedure
				await Connection.ExecuteStoredProcedureWithNoResultAsync(name, Timeout, arguments, cancellationToken);

				// close connection
				await Connection.CloseAsync();

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
				var telemetry = new DependencyTelemetry(DependencyTypeName, DependecyName, DependecyName, "", startTime, stopwatch.Elapsed, resultCode, success);

				// track dependency
				TelemetryClient.TrackDependency(telemetry);
			}
		}

		/// <summary>Initiates an asynchronous operation to execute the stored procedure which returns scalar value.</summary>
		/// <typeparam name="T">The type of the object returned by the query.</typeparam>
		/// <param name="name">Name of stored procedure to execute.</param>
		/// <param name="arguments">Collection of the arguments of the stored procedure.</param>
		/// <param name="readResult">Delegate to the method that reads result.</param>
		/// <param name="cancellationToken">The cancellation instruction.</param>
		/// <returns>
		/// A <see cref="Task{T}" /> object of type <typeparamref name="T" /> that represents the asynchronous operation.
		/// <see cref="Task{T}.Result" /> contains the result of the operation.
		/// </returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected async Task<T> ExecuteStoredProcedureWithScalarResultAsync<T>(String name, DbParameter[] arguments, Func<DbDataReader, T> readResult, CancellationToken cancellationToken)
		{
			var success = false;

			string resultCode = null;

			var startTime = DateTime.UtcNow;

			// start a stop watch
			var stopwatch = System.Diagnostics.Stopwatch.StartNew();

			try
			{
				// open connection
				await Connection.OpenAsync(cancellationToken);

				// call stored procedure
				var result = await Connection.ExecuteStoredProcedureWithScalarResultAsync(name, Timeout, arguments, readResult, cancellationToken);

				// close connection
				await Connection.CloseAsync();

				// stop the stop watch
				stopwatch.Stop();

				// set sucess
				success = true;

				// return
				return result;
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
				var telemetry = new DependencyTelemetry(DependencyTypeName, DependecyName, DependecyName, "", startTime, stopwatch.Elapsed, resultCode, success);

				// track dependency
				TelemetryClient.TrackDependency(telemetry);
			}
		}

		/// <summary>Initiates an asynchronous operation to execute the command that returns set of records.</summary>
		/// <typeparam name="T">Type of the objects returned by the stored procedure.</typeparam>
		/// <param name="name">Name of stored procedure to execute.</param>
		/// <param name="arguments">Collection of the arguments of the stored procedure.</param>
		/// <param name="readRecord">Delegate to the method that reads result record.</param>
		/// <param name="cancellationToken">The cancellation instruction.</param>
		/// <returns>
		/// A <see cref="Task{T}" /> object of type <typeparamref name="T" /> that represents the asynchronous operation.
		/// <see cref="Task{T}.Result" /> contains the result of the operation.
		/// </returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected async Task<List<T>> ExecuteStoredProcedureWithSetResultAsync<T>(String name, DbParameter[] arguments, Func<DbDataReader, T> readRecord, CancellationToken cancellationToken)
		{
			var success = false;

			string resultCode = null;

			var startTime = DateTime.UtcNow;

			// start a stop watch
			var stopwatch = System.Diagnostics.Stopwatch.StartNew();

			try
			{
				// open connection
				await Connection.OpenAsync(cancellationToken);

				// call stored procedure
				var result = await Connection.ExecuteStoredProcedureWithSetResultAsync(name, Timeout, arguments, readRecord, cancellationToken);

				// close connection
				await Connection.CloseAsync();

				// stop the stop watch
				stopwatch.Stop();

				// set sucess
				success = true;

				// return
				return result;
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
				var telemetry = new DependencyTelemetry(DependencyTypeName, DependecyName, DependecyName, "", startTime, stopwatch.Elapsed, resultCode, success);

				// track dependency
				TelemetryClient.TrackDependency(telemetry);
			}
		}

		#endregion
	}
}