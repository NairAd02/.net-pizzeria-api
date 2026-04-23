# Pizzeria API

API monolítica en **ASP.NET Core (.NET 10)** organizada por **módulos**, imitando
la estructura de un proyecto **NestJS**. El dominio es una pizzería: ingredientes,
pizzas, pedidos y repartidores.

> Almacenamiento: **en memoria** (`ConcurrentDictionary`). El objetivo es enfocarse
> en el patrón de organización, no en la persistencia. Es trivial cambiarlo por
> EF Core + SQLite/SQL Server más adelante.

## Mapeo rápido Nest -> ASP.NET

| Nest | ASP.NET Core |
|------|--------------|
| `@Module({ providers, controllers, imports })` | Clase estática `XxxModule` con el método de extensión `AddXxxModule(IServiceCollection)` que registra servicios en el contenedor de DI |
| `@Controller('ruta')` | `[ApiController] [Route("api/ruta")] class XxxController : ControllerBase` |
| `@Get() / @Post() / @Patch()` | `[HttpGet] / [HttpPost] / [HttpPatch]` |
| `@Injectable() service` | Clase registrada con `AddSingleton<IXxxService, XxxService>()` |
| `constructor(private svc: XxxService)` | Primary constructor: `class YyyController(IXxxService svc) : ControllerBase` |
| DTO en `dto/*.ts` | `record` en `Dtos/` |
| Entity en `*.entity.ts` | Clase en `Entities/` |
| `main.ts` (`NestFactory.create(AppModule)`) | `Program.cs` |

## Estructura de carpetas

```
src/Pizzeria.API/
├── Program.cs                      # "AppModule": importa todos los módulos
├── Modules/
│   ├── Ingredients/
│   │   ├── IngredientsModule.cs    # registra IIngredientsService en DI
│   │   ├── IngredientsController.cs
│   │   ├── IngredientsService.cs
│   │   ├── IIngredientsService.cs
│   │   ├── Entities/
│   │   │   └── Ingredient.cs
│   │   └── Dtos/
│   │       ├── CreateIngredientDto.cs
│   │       └── UpdateStockDto.cs
│   ├── Pizzas/            (depende de Ingredients)
│   ├── DeliveryPersons/   (independiente)
│   └── Orders/            (depende de Pizzas + Ingredients + DeliveryPersons)
```

Cada módulo sigue el mismo contrato: **Entities → DTOs → Interface del service →
Implementación del service → Controller → Module** (registro en DI).

## Endpoints principales

### Ingredients — `/api/ingredients`
- `GET /`            lista todos
- `GET /{code}`      obtiene uno
- `POST /`           crea
- `PATCH /{code}/stock`   suma stock

### Pizzas — `/api/pizzas`
- `GET /`            lista
- `GET /{id}`        obtiene una
- `POST /`           crea (valida que los ingredientes existan)
- `GET /{id}/cost`   calcula costo = `basePrice + Σ(pricePerUnit × qty)`

### Delivery persons — `/api/delivery-persons`
- `GET /`, `GET /{code}`, `POST /`

### Orders — `/api/orders`
- `GET /`, `GET /{id}`
- `POST /`                crea un pedido. Verifica stock de TODOS los ingredientes
  necesarios (cantidad × porciones). Si falta algo, **no** crea el pedido. Si hay
  stock, descuenta automáticamente.
- `PATCH /{id}/status`    cambia estado (`Pending`, `InPreparation`, `OnTheWay`,
  `Delivered`).
  - Al pasar a `OnTheWay`: busca un repartidor disponible, lo marca como ocupado
    y lo asigna al pedido. Si no hay disponibles, falla.
  - Al pasar a `Delivered`: libera al repartidor asignado.

## Cómo ejecutar

```powershell
dotnet run --project src/Pizzeria.API
```

La app queda en `http://localhost:5202`. El OpenAPI JSON vive en
`http://localhost:5202/openapi/v1.json` (solo en Development).

Para probar rápido, abre `src/Pizzeria.API/Pizzeria.API.http` en VS Code / Rider
y ejecuta los requests en orden (Ingredients → Pizzas → Delivery persons →
Orders).

## Qué añadiría en un proyecto real (fuera del alcance aquí)

- **Persistencia** con EF Core y un `DbContext` por módulo, o uno compartido.
- **Validación** con `FluentValidation` o `DataAnnotations` en los DTOs.
- **Manejo global de errores** con un `ExceptionHandlerMiddleware` o
  `ProblemDetails` personalizado, en lugar del `try/catch` en controllers.
- **Tests** por módulo (`Pizzeria.Ingredients.Tests`, etc.).
