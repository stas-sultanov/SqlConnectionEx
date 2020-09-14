﻿using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace SSO.SqlConnectionEx
{
	public static class SqlConnectionEx
	{
		#region Private methods

		/// <summary>Initializes a new instance of the <see cref="SqlCommand"/> to run a stored procedure using the <see cref="SqlConnection"/>.</summary>
		/// <param name="connection">Open connection to the SQL server.</param>
		/// <param name="name">Name of stored procedure to execute.</param>
		/// <param name="timeout">Time in seconds to wait for the stored proceduer to execute.</param>
		/// <param name="arguments">Collection of the arguments of the stored procedure.</param>
		/// <returns>A sql command.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static SqlCommand CreateStoredProcedureCommand(this SqlConnection connection, String name, Int32 timeout, SqlParameter[] arguments)
		{
			// create command
			var result = new SqlCommand(name, connection)
			{
				// set command type
				CommandType = CommandType.StoredProcedure,

				// set stored procedure timeout
				CommandTimeout = timeout,
			};

			// add arguments
			result.Parameters.AddRange(arguments);

			// execute command
			return result;
		}

		#endregion

		#region Methods

		/// <summary>Initiates an asynchronous operation to execute the stored procedure which returns nothing.</summary>
		/// <param name="connection">Open connection to the SQL server.</param>
		/// <param name="name">Name of stored procedure to execute.</param>
		/// <param name="timeout">Time in seconds to wait for the stored proceduer to execute.</param>
		/// <param name="arguments">Collection of the arguments of the stored procedure.</param>
		/// <param name="cancellationToken">The cancellation instruction.</param>
		/// <returns>A <see cref="Task" /> object that represents the asynchronous operation.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async Task ExecuteStoredProcedureAsync(this SqlConnection connection, [NotNull] String name, Int32 timeout, [NotNull] SqlParameter[] arguments, CancellationToken cancellationToken)
		{
			// create stored procedure command
			using var command = connection.CreateStoredProcedureCommand(name, timeout, arguments);

			// execute command
			await command.ExecuteNonQueryAsync(cancellationToken);
		}

		/// <summary>Initiates an asynchronous operation to execute the stored procedure which returns scalar value.</summary>
		/// <typeparam name="T">The type of the object returned by the query.</typeparam>
		/// <param name="connection">Open connection to the SQL server.</param>
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
		public static async Task<T> ExecuteStoredProcedureWithScalarResultAsync<T>(this SqlConnection connection, [NotNull] String name, Int32 timeout, [NotNull] SqlParameter[] arguments, [NotNull] Func<SqlDataReader, T> readResult, CancellationToken cancellationToken)
		{
			var result = default(T);

			// create stored procedure command
			using var command = connection.CreateStoredProcedureCommand(name, timeout, arguments);

			// execute command
			using var reader = await command.ExecuteReaderAsync(cancellationToken);

			// read while there is something
			while (await reader.ReadAsync(cancellationToken))
			{
				// create an instance of type T
				result = readResult(reader);
			}

			return result;
		}

		/// <summary>Initiates an asynchronous operation to execute the command that returns set of records.</summary>
		/// <typeparam name="T">Type of the objects returned by the stored procedure.</typeparam>
		/// <param name="connection">Open connection to the SQL server.</param>
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
		public static async Task<List<T>> ExecuteStoredProcedureWithSetResultAsync<T>(this SqlConnection connection, [NotNull] String name, Int32 timeout, [NotNull] SqlParameter[] arguments, [NotNull] Func<SqlDataReader, T> readRecord, CancellationToken cancellationToken)
		{
			// create result
			var result = new List<T>();

			// create stored procedure command
			using var command = connection.CreateStoredProcedureCommand(name, timeout, arguments);

			// execute command
			using var reader = await command.ExecuteReaderAsync();

			// read while there is something
			while (await reader.ReadAsync())
			{
				// create an instance of type T
				var record = readRecord(reader);

				// add to result
				result.Add(record);
			}

			return result;

			#endregion
		}
	}
}