using Craftsman.Domain.Integration.Models;
using Craftsman.Domain.Sales.Entities;

namespace Craftsman.Domain.Integration.Services;

public interface IOrderNormalizer
{
    Order Normalize(RawOrder rawOrder);
}
