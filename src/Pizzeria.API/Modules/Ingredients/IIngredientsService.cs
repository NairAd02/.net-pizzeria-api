using Pizzeria.API.Modules.Ingredients.Dtos;
using Pizzeria.API.Modules.Ingredients.Entities;

namespace Pizzeria.API.Modules.Ingredients;

public interface IIngredientsService
{
    IReadOnlyCollection<Ingredient> FindAll();
    Ingredient? FindByCode(string code);
    Ingredient Create(CreateIngredientDto dto);
    Ingredient AddStock(string code, decimal quantity);
    bool HasStock(string code, decimal requiredQuantity);
    void DecrementStock(string code, decimal quantity);
}
