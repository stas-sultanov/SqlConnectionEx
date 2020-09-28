using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using MySqlConnector;
using SqlConnectionEx;

namespace SSO
{
	public abstract class BaseInfrastructure
	{
		#region Data

		DbConfiguration Configuration { get; }

		TelemetryClient TelemetryClient { get; }

		#endregion

		#region Constructor

		protected BaseInfrastructure(DbConfiguration configuration, TelemetryClient telemetry)
		{
			Configuration = configuration;

			TelemetryClient = telemetry;
		}

		#endregion

		#region Methods

		/// <summary>Get connection to the database based on some logic.</summary>
		/// <param name="dependencyName">Name of the dependency to track.</param>
		/// <returns>Connection to the database.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private MySqlConnection GetConnection(out String dependencyName)
		{
			var result = new MySqlConnection(Configuration.MainConnectionString);

			dependencyName = String.Join(" | ", result.DataSource, result.Database);

			return result;
		}

		/// <summary>Send information about the database call in the application.</summary>
		/// <param name="name">Name of the stored procedure.</param>
		/// <param name="startTime">The time when the database was called.</param>
		/// <param name="stopwatch">The time taken by the database to handle the call.</param>
		/// <param name="resultCode">Result code of the stored procedure call execution.</param>
		/// <param name="success"><c>true</c> if the call was handled successfully.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void TrackStoredProcedureCall(String dependencyName, String name, DateTime startTime, System.Diagnostics.Stopwatch stopwatch, string resultCode, bool success)
		{
			var telemetry = new DependencyTelemetry
			(
				Configuration.DependencyTypeName,
				dependencyName,
				name,
				"",
				startTime,
				stopwatch.Elapsed,
				resultCode,
				success
			);

			// track dependency
			TelemetryClient.TrackDependency(telemetry);
		}

		/// <summary>Execute stored procedure procedure and track dependency to AppInsights</summary>
		/// <typeparam name="TResult">Result of Func</typeparam>
		/// <param name="commandText">Text of SQL command</param>
		/// <param name="commandCall">A func that calls a SQL command.</param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private async Task ExecuteStoredProcedureAndTrackDependencyAsync(Func<DbConnection, CancellationToken, Task> commandCall, CancellationToken cancellationToken)
		{
			var connection = GetConnection(out var dependencyName);

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
				TrackStoredProcedureCall(dependencyName, "", startTime, stopwatch, resultCode, success);
			}
		}

		/// <summary>Initiates an asynchronous operation to execute the stored procedure which returns nothing.</summary>
		/// <param name="name">Name of stored procedure to execute.</param>
		/// <param name="arguments">Collection of the arguments of the stored procedure.</param>
		/// <param name="cancellationToken">The cancellation instruction.</param>
		/// <returns>A <see cref="Task" /> object that represents the asynchronous operation.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected async Task ExecuteStoredProcedureWithNoResultAsync(String name, IEnumerable<DbParameter> arguments, CancellationToken cancellationToken)
		{
			var connection = GetConnection(out var dependencyName);

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
				await connection.ExecuteStoredProcedureWithNoResultAsync(name, Configuration.ConnectionTimeout, arguments, cancellationToken);

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
				TrackStoredProcedureCall(dependencyName, name, startTime, stopwatch, resultCode, success);
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
		protected async Task<T> ExecuteStoredProcedureWithScalarResultAsync<T>(String name, IEnumerable<DbParameter> arguments, Func<DbDataReader, T> readResult, CancellationToken cancellationToken)
		{
			var connection = GetConnection(out var dependencyName);

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
				var result = await connection.ExecuteStoredProcedureWithScalarResultAsync(name, Configuration.ConnectionTimeout, arguments, readResult, cancellationToken);

				// close connection
				await connection.CloseAsync();

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
				TrackStoredProcedureCall(dependencyName, name, startTime, stopwatch, resultCode, success);
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
		protected async Task<List<T>> ExecuteStoredProcedureWithSetResultAsync<T>(String name, IEnumerable<DbParameter> arguments, Func<DbDataReader, T> readRecord, CancellationToken cancellationToken)
		{
			var connection = GetConnection(out var dependencyName);

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
				var result = await connection.ExecuteStoredProcedureWithSetResultAsync(name, Configuration.ConnectionTimeout, arguments, readRecord, cancellationToken);

				// close connection
				await connection.CloseAsync();

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
				TrackStoredProcedureCall(dependencyName, name, startTime, stopwatch, resultCode, success);
			}
		}

		#endregion
	}
}