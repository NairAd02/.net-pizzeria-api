# Pizzeria API

API monolítica en **ASP.NET Core (.NET 10)** organizada por **módulos**, imitando
la estructura de un proyecto **NestJS**. El dominio es una pizzería: ingredientes,
pizzas, pedidos y repartidores.

> Persistencia: **PostgreSQL** vía **EF Core** (provider `Npgsql`). La conexión
> se configura por variables de entorno cargadas desde un `.env` en la raíz
> (igual que un proyecto Nest).

## Mapeo rápido Nest -> ASP.NET

| Nest | ASP.NET Core |
|------|--------------|
| `@Module({ providers, controllers, imports })` | Clase estática `XxxModule` con el método de extensión `AddXxxModule(IServiceCollection)` que registra servicios en el contenedor de DI |
| `@Controller('ruta')` | `[ApiController] [Route("api/ruta")] class XxxController : ControllerBase` |
| `@Get() / @Post() / @Patch()` | `[HttpGet] / [HttpPost] / [HttpPatch]` |
| `@Injectable() service` | Clase registrada con `AddScoped<IXxxService, XxxService>()` (scope por request, igual que Nest) |
| `constructor(private svc: XxxService)` | Primary constructor: `class YyyController(IXxxService svc) : ControllerBase` |
| DTO en `dto/*.ts` | `record` en `Dtos/` |
| Entity en `*.entity.ts` | Clase en `Entities/` (EF Core la mapea a una tabla vía `OnModelCreating`) |
| `TypeOrmModule.forRoot(...)` | `AddDbContext<PizzeriaDbContext>(options => options.UseNpgsql(...))` en `Infrastructure/Database/DatabaseModule.cs` |
| `@InjectRepository(Entity)` | Inyectar `PizzeriaDbContext` y usar `context.Xs` |
| `.env` cargado por `@nestjs/config` | `.env` cargado al inicio de `Program.cs` con `DotNetEnv` |
| `main.ts` (`NestFactory.create(AppModule)`) | `Program.cs` |

## Estructura de carpetas

```
src/Pizzeria.API/
├── Program.cs                          # "AppModule": carga .env y registra módulos
├── Infrastructure/
│   └── Database/
│       ├── PizzeriaDbContext.cs        # DbSets + OnModelCreating (relaciones)
│       └── DatabaseModule.cs           # AddDbContext con Npgsql + lectura de .env
├── Modules/
│   ├── Ingredients/
│   │   ├── IngredientsModule.cs        # registra IIngredientsService en DI
│   │   ├── IngredientsController.cs
│   │   ├── IngredientsService.cs
│   │   ├── IIngredientsService.cs
│   │   ├── Entities/
│   │   │   └── Ingredient.cs
│   │   └── Dtos/
│   │       ├── CreateIngredientDto.cs
│   │       └── UpdateStockDto.cs
│   ├── Pizzas/            (entidades: Pizza, PizzaIngredient)
│   ├── DeliveryPersons/   (entidades: DeliveryPerson)
│   └── Orders/            (entidades: Order, OrderItem — orquesta stock + repartidores)
```

Los services simples (`Ingredients`, `Pizzas`, `DeliveryPersons`) sólo leen/escriben
sus propias entidades. `OrdersService` es el orquestador: abre una transacción y
toca `Pizzas`, `Ingredients` y `DeliveryPersons` en un solo `SaveChangesAsync`.

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

## Setup

### 1. Requisitos

- **.NET 10 SDK**
- **PostgreSQL** corriendo localmente (o donde sea; sólo necesitas las credenciales)
- CLI de EF Core: `dotnet tool install --global dotnet-ef` (ya lo tienes si
  `dotnet ef --version` devuelve 10.x)

### 2. Variables de entorno

Copia `.env.example` a `.env` en la raíz del repo y pon tus valores:

```env
POSTGRES_HOST=localhost
POSTGRES_PORT=5432
POSTGRES_DB=pizzeria
POSTGRES_USER=postgres
POSTGRES_PASSWORD=tu_password
```

> `DotNetEnv` carga este archivo al arranque, tanto en runtime como cuando
> corres `dotnet ef` (para migrations).

Asegúrate de que la base de datos (`pizzeria` en el ejemplo) **exista** en tu
Postgres antes de aplicar migraciones. Puedes crearla con:

```powershell
psql -U postgres -c "CREATE DATABASE pizzeria;"
```

### 3. Migraciones

Desde la raíz del repo:

```powershell
# Crear la primera migración
dotnet ef migrations add InitialCreate --project src/Pizzeria.API

# Aplicar al servidor (crea las tablas)
dotnet ef database update --project src/Pizzeria.API
```

Cada vez que cambies una entidad o el `OnModelCreating`, repite:

```powershell
dotnet ef migrations add DescribeTheChange --project src/Pizzeria.API
dotnet ef database update --project src/Pizzeria.API
```

### 4. Ejecutar la app

```powershell
dotnet run --project src/Pizzeria.API
```

La API queda en `http://localhost:5202`. El OpenAPI JSON vive en
`http://localhost:5202/openapi/v1.json` (solo en Development).

Para probar rápido, abre `src/Pizzeria.API/Pizzeria.API.http` en VS Code / Rider
y ejecuta los requests en orden (Ingredients → Pizzas → Delivery persons →
Orders).

## Detalles de EF Core que conviene tener presentes

- **Scope**: el `PizzeriaDbContext` es `Scoped` (una instancia por request).
  Por eso todos los services se registran con `AddScoped`.
- **`AsNoTracking()`** en las lecturas: EF no trackea las entidades, más rápido
  y sin riesgo de "auto-guardar" cambios por error.
- **Enums como string**: `OrderStatus` y `DeliveryPersonStatus` se guardan como
  texto (`"Pending"`, `"OnTheWay"`...) con `HasConversion<string>()`. Los
  clientes también los ven así gracias a `JsonStringEnumConverter`.
- **Transacciones explícitas** en `OrdersService`: `CreateAsync` y
  `UpdateStatusAsync` abren una transacción con
  `context.Database.BeginTransactionAsync` para que verificar stock, descontar
  stock, crear el pedido y asignar/liberar repartidor sean una operación
  atómica.
- **FK y borrado**: si borras un ingrediente que está en alguna pizza, o una
  pizza que está en algún pedido, EF lo **restringe** (no se borra). Los
  `PizzaIngredient` y `OrderItem` sí se borran en cascada con su padre.

## Qué añadiría en un proyecto real (fuera del alcance aquí)

- **Validación** con `FluentValidation` o `DataAnnotations` en los DTOs.
- **Manejo global de errores** con un `ExceptionHandlerMiddleware` o
  `ProblemDetails` personalizado, en lugar del `try/catch` en controllers.
- **Tests** por módulo (`Pizzeria.Ingredients.Tests`, etc.) usando la
  `Microsoft.EntityFrameworkCore.InMemory` para los services.
