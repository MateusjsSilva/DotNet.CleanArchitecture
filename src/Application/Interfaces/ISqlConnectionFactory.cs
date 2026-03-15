using System.Data;

namespace CleanArchitecture.Application.Interfaces;

public interface ISqlConnectionFactory
{
    IDbConnection CreateConnection();
}
