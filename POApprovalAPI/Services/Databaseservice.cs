using Microsoft.Data.SqlClient;
using System.Data;

namespace POApprovalAPI.Services
{
    public class DatabaseService
    {
        private readonly IConfiguration _configuration;

        public DatabaseService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public SqlConnection CreateConnection()
        {
            var connection = new SqlConnection(
                _configuration.GetConnectionString("DefaultConnection")
            );

            connection.Open();
            return connection;
        }

        public SqlConnection CreateLoginEntryConnection()
        {
            var connection = new SqlConnection(
                _configuration.GetConnectionString("LoginEntryConnection")
            );

            connection.Open();
            return connection;
        }

        /// <summary>
        /// Live payroll / attendance Loginentry on NEWERP MSSQLPAYROLL (port 3445).
        /// Do not use the archive LoginEntryConnection (port 5115) for HR punches.
        /// </summary>
        public SqlConnection CreatePayrollLoginEntryConnection()
        {
            var connection = new SqlConnection(
                _configuration.GetConnectionString("PayrollLoginEntryConnection")
            );

            connection.Open();
            return connection;
        }

        public SqlConnection CreateProductionConnection()
        {
            var connection = new SqlConnection(
                _configuration.GetConnectionString("ProductionConnection")
            );

            connection.Open();
            return connection;
        }

        /// <summary>
        /// Safely closes and disposes a connection, returning it to the connection pool
        /// </summary>
        public void CloseConnection(SqlConnection connection)
        {
            if (connection != null)
            {
                try
                {
                    if (connection.State == ConnectionState.Open)
                    {
                        connection.Close(); // Returns to pool
                    }
                }
                finally
                {
                    connection?.Dispose(); // Cleanup resources
                }
            }
        }
    }
}