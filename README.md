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
│   ├── Database/
│   │   ├── PizzeriaDbContext.cs        # DbSets + OnModelCreating (relaciones)
│   │   └── DatabaseModule.cs           # AddDbContext con Npgsql + lectura de .env
│   └── Storage/
│       ├── ProductImage.cs             # tipo tipado que se guarda dentro de la columna jsonb
│       ├── IStorageService.cs          # contrato provider-agnostic
│       ├── S3StorageService.cs         # implementación sobre AWSSDK.S3 (funciona con cualquier S3-compatible)
│       ├── StorageOptions.cs           # POCO poblado desde STORAGE_* del .env
│       ├── ImageFileValidator.cs       # validación compartida (tamaño, content-type)
│       └── StorageModule.cs            # AddStorageModule: registra IAmazonS3 + IStorageService
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
- `GET /`                         lista todos
- `GET /{code}`                   obtiene uno
- `POST /`                        crea
- `PATCH /{code}/stock`           suma stock
- `POST /{code}/images`           sube una imagen (multipart: `file`, opcional `altText`)
- `DELETE /{code}/images/{key}`   elimina una imagen del storage y del JSON

### Pizzas — `/api/pizzas`
- `GET /`                       lista
- `GET /{id}`                   obtiene una
- `POST /`                      crea (valida que los ingredientes existan)
- `GET /{id}/cost`              calcula costo = `basePrice + Σ(pricePerUnit × qty)`
- `POST /{id}/images`           sube una imagen (multipart: `file`, opcional `altText`)
- `DELETE /{id}/images/{key}`   elimina una imagen del storage y del JSON

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

En el mismo `.env` añade el bloque `STORAGE_*`. El proyecto ya trae el módulo
`Infrastructure/Storage` listo para hablar con cualquier proveedor S3-compatible;
cambiar de Supabase a AWS S3, Cloudflare R2 o MinIO es **solo editar el `.env`**
(cero cambios en C#). Ver la sección [Storage de imágenes](#storage-de-imágenes)
más abajo para el detalle de cada variable.

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

## Storage de imágenes

Las entidades `Pizza` e `Ingredient` tienen una columna `images` de tipo `jsonb`
que guarda una lista de objetos `ProductImage` (definido en
`Infrastructure/Storage/ProductImage.cs`). Cada objeto lleva:

```json
{
  "key": "pizzas/<id>/<guid>.jpg",
  "url": "https://.../<bucket>/pizzas/<id>/<guid>.jpg",
  "contentType": "image/jpeg",
  "size": 184213,
  "width": null,
  "height": null,
  "altText": "Vista superior de la Margherita",
  "createdAt": "2025-10-20T18:32:11.123Z"
}
```

### Arquitectura

- `IStorageService` (contrato agnóstico): `UploadAsync`, `DeleteAsync`, `BuildPublicUrl`.
- `S3StorageService` (implementación única): usa `AWSSDK.S3` con `ServiceURL` +
  `ForcePathStyle`, por lo que sirve para **cualquier** backend S3-compatible:
  Supabase, AWS S3, Cloudflare R2, MinIO, Wasabi, Backblaze B2, etc.
- `StorageModule.AddStorageModule()` registra el cliente S3 como singleton y
  lee las `STORAGE_*` del `.env`.

Los módulos de dominio (`Pizzas`, `Ingredients`) solo dependen de la interfaz,
nunca del SDK.

### Configuración

Bloque `STORAGE_*` en `.env` (ver `.env.example` para las plantillas completas
de Supabase, AWS, R2 y MinIO):

| Variable | Requerida | Descripción |
|---|---|---|
| `STORAGE_PROVIDER` | no | Etiqueta informativa (`supabase`, `aws`, `r2`, `minio`). |
| `STORAGE_ENDPOINT` | **sí** | URL del servicio S3. Ej. `https://<ref>.supabase.co/storage/v1/s3`. |
| `STORAGE_REGION` | no | Default `us-east-1`. En R2 usa `auto`. |
| `STORAGE_ACCESS_KEY_ID` | **sí** | Credencial S3. |
| `STORAGE_SECRET_ACCESS_KEY` | **sí** | Credencial S3. |
| `STORAGE_BUCKET` | **sí** | Nombre del bucket (debe existir, configurado como público). |
| `STORAGE_PUBLIC_BASE_URL` | no | URL base para construir las URLs finales. Si no se define se usa `{endpoint}/{bucket}`. |
| `STORAGE_FORCE_PATH_STYLE` | no | Default `true`. Solo se pone `false` para AWS S3 con virtual-host. |
| `STORAGE_MAX_FILE_SIZE_MB` | no | Default `5`. Valida el tamaño por archivo. |
| `STORAGE_ALLOWED_CONTENT_TYPES` | no | Default `image/jpeg,image/png,image/webp,image/avif`. |

Para **Supabase** concretamente:

1. En el dashboard de Supabase crea un bucket (p. ej. `pizzeria-images`) y
   márcalo como **Public**.
2. En _Project Settings → Storage_ obtén las credenciales **S3 access keys** y
   el **S3 connection URL** (el endpoint).
3. Pon los valores en `.env` usando el bloque de ejemplo.

Para **cambiar a otro proveedor**: solo reemplaza el bloque `STORAGE_*` con
el de la plantilla correspondiente en `.env.example`. No se toca ni una línea
de C#.

### Endpoints

```
POST   /api/pizzas/{id}/images          multipart: file, altText?
DELETE /api/pizzas/{id}/images/{key}    key = "pizzas/{id}/{guid}.ext"

POST   /api/ingredients/{code}/images          multipart: file, altText?
DELETE /api/ingredients/{code}/images/{key}    key = "ingredients/{code}/{guid}.ext"
```

Los `GET` de pizzas/ingredients ya devuelven la columna `images` poblada
automáticamente.

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
