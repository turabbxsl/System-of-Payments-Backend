using Dapper;
using Npgsql;
using Payments.Common.Models;
using PaymentsPublisher.Models;
using System.Data;

namespace PaymentsPublisher.Services
{
    public interface IDbService
    {
        Task<Result<List<OutboxMessage>>> GetUnprocessedMessagesAsync(int rowCount);
        Task<Result<bool>> UpdateMessageAsync(Guid Id);
    }

    public class DbService : IDbService
    {
        private readonly string _connectionString;

        public DbService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                            ?? throw new InvalidOperationException("DefaultConnection string is missing in configuration.");
        }

        public async Task<Result<List<OutboxMessage>>> GetUnprocessedMessagesAsync(int rowCount)
        {
            const string query = @" select * 
                                    from public.InProgressPayments
                                    order by createdate
                                    limit @rowCount";

            try
            {
                using var connection = GetConnection();

                var dbResult = await connection.QueryAsync<OutboxMessage>(
                    sql: query,
                    param: new { rowCount = rowCount }
                );

                var messages = dbResult.ToList();

                return Result.Success(messages);

            }
            catch (NpgsqlException ex)
            {
                return Result.Failure<List<OutboxMessage>>(Error.DatabaseFailure with
                {
                    Message = $"Database read failed: {ex.Message}"
                });
            }
            catch (Exception ex)
            {
                return Result.Failure<List<OutboxMessage>>(Error.Failure with
                {
                    Message = $"An unexpected error occurred while fetching outbox messages: {ex.Message}"
                });
            }
        }

        public async Task<Result<bool>> UpdateMessageAsync(Guid id)
        {

            const string updatePaymentQuery = @"UPDATE public.payments
                                                SET status = 'Pending',
                                                    updatedat = now()
                                                WHERE id = @Id";

            const string updateOutboxQuery = @"UPDATE public.outboxmessages
                                               SET status = 'Pending',
                                                    updatedat = now()
                                               WHERE transactionId = @Id";

            using var connection = GetConnection();

            try
            {
                if (connection.State != ConnectionState.Open)
                {
                    await ((NpgsqlConnection)connection).OpenAsync();
                }

                using var transaction = connection.BeginTransaction();

                var paymentRowsAffected = await connection.ExecuteAsync(
                    sql: updatePaymentQuery,
                    param: new { Id = id },
                    transaction: transaction);

                if (paymentRowsAffected == 0)
                {
                    return Result.Failure<bool>(Error.NotFound with
                    {
                        Message = $"Payment with Id: {id} not found or update failed."
                    });
                }

                var outboxRowsAffected = await connection.ExecuteAsync(
                    sql: updateOutboxQuery,
                    param: new { Id = id },
                    transaction: transaction);

                if (outboxRowsAffected == 0)
                {
                    return Result.Failure<bool>(Error.Failure with
                    {
                        Message = $"Outbox message with Id: {id} not found or update failed. Transaction aborted."
                    });
                }

                transaction.Commit();
                return Result.Success(true);
            }
            catch (NpgsqlException ex)
            {
                return Result.Failure<bool>(Error.DatabaseFailure with
                {
                    Message = $"Transaction failed due to database error: {ex.Message}"
                });
            }
            catch (Exception ex)
            {
                return Result.Failure<bool>(Error.Failure with
                {
                    Message = $"An unexpected error occurred during message update: {ex.Message}"
                });
            }
        }

        private IDbConnection GetConnection()
        {
            return new NpgsqlConnection(_connectionString);
        }
    }
}
