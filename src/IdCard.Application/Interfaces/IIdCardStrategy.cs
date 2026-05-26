using IdCard.Application.Models;
using IdCard.Domain.Models;

namespace IdCard.Application.Interfaces;

public interface IIdCardStrategy
{
    StrategyResult Execute(IdCardContext context);
}
