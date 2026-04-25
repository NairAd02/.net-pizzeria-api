# Pizzeria API

API monolítica en **ASP.NET Core (.NET 10)** organizada por **módulos**, imitando
la estructura de un proyecto **NestJS**. El dominio es una pizzería: ingredientes,
pizzas, pedidos y repartidores.

> Persistencia: **PostgreSQL** vía **EF Core** (provider `Npgsql`). La conexión
> se configura por variables de entorno cargadas desde un `.env` en la raíz
> (igual que un proyecto Nest).
>
> Auth: **JWT Bearer** (paquete `Microsoft.AspNetCore.Authentication.JwtBearer`)
> con access + refresh tokens y roles `Admin`/`Client` sobre una tabla `users`
> propia. Password hashing con `PasswordHasher<User>` (PBKDF2 + salt, el mismo
> algoritmo que usa ASP.NET Core Identity).

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
| `@UseGuards(JwtAuthGuard)` / `@UseGuards(RolesGuard)` | `[Authorize]` / `[Authorize(Roles = Roles.Admin)]` |
| `JwtStrategy` + `passport-jwt` | `AddJwtBearer(...)` con `TokenValidationParameters` |

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
│   ├── Auth/
│   │   ├── AuthModule.cs               # AddAuthModule: DI + AddJwtBearer + AddAuthorization + seed admin
│   │   ├── AuthController.cs           # /api/auth: sign-up, sign-in, refresh, sign-out, me
│   │   ├── AuthService.cs / IAuthService.cs
│   │   ├── Roles.cs                    # constantes "Admin"/"Client" (atadas al enum con nameof)
│   │   ├── Jwt/
│   │   │   ├── JwtOptions.cs           # leído desde .env (JWT_*)
│   │   │   ├── IJwtTokenService.cs
│   │   │   └── JwtTokenService.cs      # access token firmado + refresh aleatorio hasheado SHA-256
│   │   ├── Entities/
│   │   │   ├── User.cs
│   │   │   ├── UserRole.cs             # enum { Admin, Client }
│   │   │   └── RefreshToken.cs
│   │   └── Dtos/
│   │       ├── SignUpDto.cs / SignInDto.cs / RefreshDto.cs
│   │       └── AuthResponseDto.cs / MeDto.cs
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

Todos los endpoints (salvo los de `/api/auth/sign-up`, `/sign-in` y `/refresh`)
requieren `Authorization: Bearer <accessToken>`. La columna **Rol** indica quién
puede llamarlos (ver [Autenticación](#autenticación-y-autorización) más abajo).

### Auth — `/api/auth`
| Método | Ruta | Rol | Descripción |
|---|---|---|---|
| POST | `/sign-up` | público | registra un usuario nuevo (rol `Client` forzado) y devuelve tokens |
| POST | `/sign-in` | público | login con email + password → devuelve access + refresh |
| POST | `/refresh` | público | rota el par (access, refresh); revoca el refresh viejo |
| POST | `/sign-out` | autenticado | revoca el refresh token enviado; el access expira solo |
| GET | `/me` | autenticado | datos del usuario del token (`id`, `email`, `role`) |

### Ingredients — `/api/ingredients`
| Método | Ruta | Rol |
|---|---|---|
| GET | `/`, `/{code}` | cualquier autenticado |
| POST | `/` | Admin |
| PATCH | `/{code}/stock` | Admin |
| POST | `/{code}/images` | Admin — multipart: `files` repetido, opcional `altTexts` por índice |
| DELETE | `/{code}/images/{key}` | Admin |

### Pizzas — `/api/pizzas`
| Método | Ruta | Rol |
|---|---|---|
| GET | `/`, `/{id}`, `/{id}/cost` | cualquier autenticado — el coste se calcula como `basePrice + Σ(pricePerUnit × qty)` |
| POST | `/` | Admin |
| POST | `/{id}/images` | Admin |
| DELETE | `/{id}/images/{key}` | Admin |

### Delivery persons — `/api/delivery-persons`
Todos los endpoints son **Admin**: `GET /`, `GET /{code}`, `POST /`.

### Orders — `/api/orders`
| Método | Ruta | Rol |
|---|---|---|
| GET | `/` | Admin (lista completa) |
| GET | `/{id}` | Admin **o** el Client dueño del pedido (otro Client recibe `403 Forbidden`) |
| POST | `/` | cualquier autenticado; el pedido se liga al `sub` del JWT automáticamente (el body nunca decide el dueño) |
| PATCH | `/{id}/status` | Admin — cambia estado entre `Pending` → `InPreparation` → `OnTheWay` → `Delivered` |

- `POST /` verifica stock de TODOS los ingredientes necesarios (`cantidad × porciones`).
  Si falta algo, **no** crea el pedido; si hay stock, descuenta automáticamente.
- `PATCH /{id}/status` con `OnTheWay` busca un repartidor disponible, lo marca
  como ocupado y lo asigna al pedido; con `Delivered` lo libera.

## Setup

### 1. Requisitos

- **.NET 10 SDK**
- **PostgreSQL** corriendo localmente (o donde sea; sólo necesitas las credenciales)
- CLI de EF Core: `dotnet tool install --global dotnet-ef` (ya lo tienes si
  `dotnet ef --version` devuelve 10.x)

### 2. Variables de entorno

Copia `.env.example` a `.env` en la raíz del repo y pon tus valores. Hay tres
bloques: Postgres, Auth/JWT y Storage:

```env
# --- PostgreSQL ---
POSTGRES_HOST=localhost
POSTGRES_PORT=5432
POSTGRES_DB=pizzeria
POSTGRES_USER=postgres
POSTGRES_PASSWORD=tu_password

# --- Auth / JWT ---
JWT_SECRET=<string aleatorio de >= 32 bytes>
JWT_ISSUER=pizzeria-api
JWT_AUDIENCE=pizzeria-api-clients
JWT_ACCESS_EXPIRES_MINUTES=15
JWT_REFRESH_EXPIRES_DAYS=7

# Crea el primer admin al arrancar si no hay ninguno. Idempotente.
INITIAL_ADMIN_EMAIL=admin@pizzeria.local
INITIAL_ADMIN_PASSWORD=change_me_on_first_run
```

Para generar un `JWT_SECRET` fuerte en PowerShell:

```powershell
[Convert]::ToBase64String([byte[]](1..48 | ForEach-Object { Get-Random -Maximum 256 }))
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

## Autenticación y autorización

Stack: **`Microsoft.AspNetCore.Authentication.JwtBearer`** + **`PasswordHasher<User>`**
(viene en el shared framework de .NET 10, usa PBKDF2 + salt por usuario, formato
versionado con `Rehash`). Se mantiene una tabla `users` propia en vez del stack
completo de ASP.NET Core Identity para encajar con el estilo "un módulo por
entidad" del repo.

### Flujo de tokens

- **Access token** (JWT firmado HS256, 15 min por defecto) con claims
  `sub`, `email`, `role`, `jti`. Se valida contra `JWT_SECRET`/`JWT_ISSUER`/
  `JWT_AUDIENCE` en `AuthModule`.
- **Refresh token** (string aleatorio de 64 bytes, 7 días por defecto)
  guardado en `refresh_tokens` como **SHA-256** (nunca el token en claro).
  Se **rota** en cada `/refresh`: el viejo se marca `RevokedAt` y apunta
  al nuevo vía `ReplacedByTokenId`. Si alguien reusa un refresh ya revocado,
  se revocan TODOS los refresh activos de ese usuario (detección de robo).
- **Sign-out** revoca el refresh token enviado. El access sigue vivo hasta
  expirar por tiempo — es el trade-off clásico de JWT stateless.

### Roles

El enum `UserRole { Admin, Client }` se emite como claim `role` en el JWT y se
persiste como string (`HasConversion<string>()`). La protección a nivel de
endpoint usa los atributos estándar de ASP.NET Core:

```csharp
[Authorize]                              // cualquier autenticado
[Authorize(Roles = Roles.Admin)]         // solo admin
[AllowAnonymous]                         // abre excepciones (sign-in, sign-up...)
```

`Roles.Admin`/`Roles.Client` son `const` atadas al enum con `nameof`: si
renombras un valor, el compilador te obliga a actualizar la constante.

### Admin inicial

Al arrancar, si no hay ningún usuario con rol `Admin`, `AuthModule.SeedInitialAdminAsync`
crea uno leyendo `INITIAL_ADMIN_EMAIL` e `INITIAL_ADMIN_PASSWORD` del `.env`.
Es idempotente: en arranques siguientes no hace nada. Si dejas esas variables
vacías no siembra nada y la app arranca igual.

### Prueba rápida del flujo

```http
# 1. Inicia sesión con el admin sembrado
POST /api/auth/sign-in
{ "email": "admin@pizzeria.local", "password": "change_me_on_first_run" }
# → { "accessToken": "...", "refreshToken": "...", "user": { "role": "Admin" } }

# 2. Llama a un endpoint Admin con el access token
POST /api/pizzas
Authorization: Bearer <accessToken>

# 3. Renueva sin volver a pedir password
POST /api/auth/refresh
{ "refreshToken": "<refreshToken>" }
```

Los ejemplos completos están en `src/Pizzeria.API/Pizzeria.API.http`.

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
POST   /api/pizzas/{id}/images          multipart: files[*], altTexts[*]?
DELETE /api/pizzas/{id}/images/{key}    key = "pizzas/{id}/{guid}.ext"

POST   /api/ingredients/{code}/images          multipart: files[*], altTexts[*]?
DELETE /api/ingredients/{code}/images/{key}    key = "ingredients/{code}/{guid}.ext"
```

El `POST` de imágenes acepta una **o varias** imágenes en la misma llamada:

- Repite el campo `files` por cada imagen que quieras subir.
- Opcionalmente repite el campo `altTexts`; el i-ésimo `altText` se empareja
  con el i-ésimo `file`. Si mandas menos `altTexts` que `files`, los que falten
  quedan sin alt.
- Si **cualquier** archivo falla validación (tamaño, content-type), se rechaza
  toda la request sin subir nada.
- Si un upload al storage falla a mitad de camino, los archivos que ya se
  habían subido se borran del storage (best-effort) antes de propagar el error.

Los `GET` de pizzas/ingredients ya devuelven la columna `images` poblada
automáticamente.

## Detalles de EF Core que conviene tener presentes

- **Scope**: el `PizzeriaDbContext` es `Scoped` (una instancia por request).
  Por eso todos los services se registran con `AddScoped`.
- **`AsNoTracking()`** en las lecturas: EF no trackea las entidades, más rápido
  y sin riesgo de "auto-guardar" cambios por error.
- **Enums como string**: `OrderStatus`, `DeliveryPersonStatus` y `UserRole` se
  guardan como texto (`"Pending"`, `"OnTheWay"`, `"Admin"`...) con
  `HasConversion<string>()`. Los clientes también los ven así gracias a
  `JsonStringEnumConverter`.
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
- **Auth endurecida**: recuperación de contraseña por email, confirmación de
  email, lockout por intentos fallidos, 2FA, y revocación inmediata del access
  token vía denylist de `jti` (p. ej. en Redis) si la política lo exige.
