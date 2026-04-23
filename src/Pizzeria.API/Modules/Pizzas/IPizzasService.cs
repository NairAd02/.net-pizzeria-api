using Pizzeria.API.Modules.Pizzas.Dtos;
using Pizzeria.API.Modules.Pizzas.Entities;

namespace Pizzeria.API.Modules.Pizzas;

public interface IPizzasService
{
    IReadOnlyCollection<Pizza> FindAll();
    Pizza? FindById(string id);
    Pizza Create(CreatePizzaDto dto);
    PizzaCostDto CalculateCost(string id);
}
