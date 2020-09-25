using Microsoft.ApplicationInsights.DataContracts;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace SSO.SqlConnectionEx
{
	using SqlConnection = MySqlConnector.MySqlConnection;
	using SqlCommand = MySqlConnector.MySqlCommand;
	using SqlParameter = MySqlConnector.MySqlParameter;
	using SqlDataReader = MySqlConnector.MySqlDataReader;

	public static class SqlConnectionEx
	{
		#region Private methods

		/// <summary>
		/// Tracks MySQL Dependency into AppInsights.
		/// </summary>
		/// <typeparam name="TResult">Result of Func</typeparam>
		/// <param name="commandText">Text of SQL command</param>
		/// <param name="commandCall">A func that calls a SQL command.</param>
		/// <returns></returns>
		private static async Task<TResult> TrackDependency<TResult>(Action<Task, SqlConnection, CancellationToken> commandCall)
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
				var telemetry = new DependencyTelemetry("MySQL", DependencyTarget, DependencyTarget, sql, startTime, timer.Elapsed, resultCode, success);

				var sql = commandText.Substring(0, Math.Min(1000, commandText.Length));

				TelemetryClient telemetry = null;

				telemetry?.TrackDependency();
			}

			return resultCode;
		}

		/// <summary>Initializes a new instance of the <see cref="SqlCommand"/> to run a stored procedure using the <see cref="SqlConnection"/>.</summary>
		/// <param name="connection">Connection to the SQL database.</param>
		/// <param name="name">Name of stored procedure to execute.</param>
		/// <param name="timeout">Time in seconds to wait for the stored proceduer to execute.</param>
		/// <param name="arguments">Collection of the arguments of the stored procedure.</param>
		/// <returns>A sql command.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static SqlCommand CreateStoredProcedureCommand(this SqlConnection connection, String name, Int32 timeout, SqlParameter[] arguments)
		{
			// create command
			var result = connection.CreateCommand();

			// set command type
			result.CommandType = CommandType.StoredProcedure;

			// set stored procedure name to call
			result.CommandText = name;

			// set stored procedure timeout
			result.CommandTimeout = timeout;

			// add arguments
			result.Parameters.AddRange(arguments);

			// execute command
			return result;
		}

		#endregion

		#region Methods

		/// <summary>Initiates an asynchronous operation to execute the stored procedure which returns nothing.</summary>
		/// <param name="connection">Connection to the SQL database.</param>
		/// <param name="name">Name of stored procedure to execute.</param>
		/// <param name="timeout">Time in seconds to wait for the stored proceduer to execute.</param>
		/// <param name="arguments">Collection of the arguments of the stored procedure.</param>
		/// <param name="cancellationToken">The cancellation instruction.</param>
		/// <returns>A <see cref="Task" /> object that represents the asynchronous operation.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async Task ExecuteStoredProcedureAsync(this SqlConnection connection, String name, Int32 timeout, SqlParameter[] arguments, CancellationToken cancellationToken)
		{
			// create stored procedure command
			using var command = connection.CreateStoredProcedureCommand(name, timeout, arguments);

			// execute command
			await command.ExecuteNonQueryAsync(cancellationToken);
		}

		/// <summary>Initiates an asynchronous operation to execute the stored procedure which returns scalar value.</summary>
		/// <typeparam name="T">The type of the object returned by the query.</typeparam>
		/// <param name="connection">Connection to the SQL database.</param>
		/// <param name="name">Name of stored procedure to execute.</param>
		/// <param name="timeout">Time in seconds to wait for the stored proceduer to execute.</param>
		/// <param name="arguments">Collection of the arguments of the stored procedure.</param>
		/// <param name="readResult">Delegate to the method that reads result.</param>
		/// <param name="cancellationToken">The cancellation instruction.</param>
		/// <returns>
		/// A <see cref="Task{T}" /> object of type <typeparamref name="T" /> that represents the asynchronous operation.
		/// <see cref="Task{T}.Result" /> contains the result of the operation.
		/// </returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async Task<T> ExecuteStoredProcedureWithScalarResultAsync<T>(this SqlConnection connection, String name, Int32 timeout, SqlParameter[] arguments, Func<SqlDataReader, T> readResult, CancellationToken cancellationToken)
		{
			var result = default(T);

			// create stored procedure command
			using var command = connection.CreateStoredProcedureCommand(name, timeout, arguments);

			// execute command
			using var reader = await command.ExecuteReaderAsync(cancellationToken);

			// read result
			if (await reader.ReadAsync(cancellationToken))
			{
				// create an instance of type T
				result = readResult(reader);
			}

			return result;
		}

		/// <summary>Initiates an asynchronous operation to execute the command that returns set of records.</summary>
		/// <typeparam name="T">Type of the objects returned by the stored procedure.</typeparam>
		/// <param name="connection">Connection to the SQL database.</param>
		/// <param name="name">Name of stored procedure to execute.</param>
		/// <param name="timeout">Time in seconds to wait for the stored proceduer to execute.</param>
		/// <param name="arguments">Collection of the arguments of the stored procedure.</param>
		/// <param name="readResult">Delegate to the method that reads result.</param>
		/// <param name="cancellationToken">The cancellation instruction.</param>
		/// <returns>
		/// A <see cref="Task{T}" /> object of type <typeparamref name="T" /> that represents the asynchronous operation.
		/// <see cref="Task{T}.Result" /> contains the result of the operation.
		/// </returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async Task<List<T>> ExecuteStoredProcedureWithSetResultAsync<T>(this SqlConnection connection, String name, Int32 timeout, SqlParameter[] arguments, Func<SqlDataReader, T> readRecord, CancellationToken cancellationToken)
		{
			// create result
			var result = new List<T>();

			// create stored procedure command
			using var command = connection.CreateStoredProcedureCommand(name, timeout, arguments);

			// execute command
			using var reader = await command.ExecuteReaderAsync(cancellationToken);

			// read while there is something
			while (await reader.ReadAsync(cancellationToken))
			{
				// create an instance of type T
				var record = readRecord(reader);

				// add to result
				result.Add(record);
			}

			return result;
		}

		#endregion
	}
}
