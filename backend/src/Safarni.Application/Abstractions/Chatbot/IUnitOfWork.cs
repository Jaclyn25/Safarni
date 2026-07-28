using System.Threading;
using System.Threading.Tasks;

namespace ChatBotApi.Application.Common.Interfaces;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
