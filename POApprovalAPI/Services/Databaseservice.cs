using Microsoft.Data.SqlClient;

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
    }
}