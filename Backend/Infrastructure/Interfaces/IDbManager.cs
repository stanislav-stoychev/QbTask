using System.Data.Common;

namespace Backend.Infrastructure.Interfaces;

public interface IDbManager
{
    DbConnection GetConnection();
}
