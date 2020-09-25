using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace SSO
{
	public static class DbConnectionEx
	{
		#region Private methods

		/// <summary>Initializes a new instance of the data base command to run a stored procedure using the <see cref="IDbConnection"/>.</summary>
		/// <param name="connection">Connection to the database.</param>
		/// <param name="name">Name of stored procedure to execute.</param>
		/// <param name="timeout">Time in seconds to wait for the stored proceduer to execute.</param>
		/// <param name="arguments">Collection of the arguments of the stored procedure.</param>
		/// <returns>A data base command.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static DbCommand CreateStoredProcedureCommand(this DbConnection connection, String name, Int32 timeout, DbParameter[] arguments)
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
			foreach (var argument in arguments)
			{
				result.Parameters.Add(argument);
			}

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
		public static async Task ExecuteStoredProcedureWithNoResultAsync(this DbConnection connection, String name, Int32 timeout, DbParameter[] arguments, CancellationToken cancellationToken)
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
		public static async Task<T> ExecuteStoredProcedureWithScalarResultAsync<T>(this DbConnection connection, String name, Int32 timeout, DbParameter[] arguments, Func<DbDataReader, T> readResult, CancellationToken cancellationToken)
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
		/// <param name="readRecord">Delegate to the method that reads result record.</param>
		/// <param name="cancellationToken">The cancellation instruction.</param>
		/// <returns>
		/// A <see cref="Task{T}" /> object of type <typeparamref name="T" /> that represents the asynchronous operation.
		/// <see cref="Task{T}.Result" /> contains the result of the operation.
		/// </returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async Task<List<T>> ExecuteStoredProcedureWithSetResultAsync<T>(this DbConnection connection, String name, Int32 timeout, DbParameter[] arguments, Func<DbDataReader, T> readRecord, CancellationToken cancellationToken)
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