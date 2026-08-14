# eShop — Microservicios

Solución de microservicios en .NET (Minimal APIs + Carter + MediatR/CQRS) con frontend en Vue 3.

## Arquitectura

```
eshop-service/
├─ src/
│  ├─ BuildingBlocks/BuildingBlocks/   Código compartido: CQRS (ICommand/IQuery), behaviors
│  │                                   (Validation/Logging), excepciones base y su exception
│  │                                   handler (ProblemDetails, sin stack traces).
│  ├─ Catalog.API/                     Catálogo de productos. Marten sobre PostgreSQL (Neon).
│  ├─ Basket.API/                      Carrito de compras. Marten sobre PostgreSQL + caché en Redis.
│  └─ Orders.API/                      NUEVO. Genera y consulta órdenes a partir del Basket.
│                                       Persistencia exclusiva en MongoDB Atlas.
├─ docker-compose.yml / .override.yml
└─ eshop-service.sln

eshop-vue-front/          Frontend real (Vue 3 + Pinia + Vite). Consume las 3 APIs por HTTP.
eshop-service-front/      Proyecto Angular sin usar, no se tocó.
```

Todos los servicios siguen el mismo patrón: **Carter** para las Minimal API, **MediatR** para
CQRS (`ICommand`/`IQuery` de `BuildingBlocks`), **FluentValidation** para validar comandos, y
`CustomExceptionHandler` de `BuildingBlocks` para traducir excepciones a `ProblemDetails` (400 /
404 / 500) sin exponer stack traces.

Orders.API no referencia el proyecto Basket.API: le habla por HTTP, como un servicio real le
hablaría a otro. Cada uno es dueño de su propio contrato de datos.

## Usuarios ("Cambiar usuario")

No hay login. El frontend tiene una pestaña **"Cambiar usuario: `<nombre>`"** en la barra de
navegación: al hacer clic se convierte en un input; escribes un nombre y das Enter. Ese nombre:

1. Se guarda en la base de datos vía `POST /customers` en **Basket.API** (colección `Customer`
   en el mismo Postgres/Marten que ya usa Basket) — `src/Basket.API/Customers/SwitchUser/`.
2. Pasa a ser el identificador único (`UserName` en Basket, `CustomerId` en Orders) con el que se
   guardan **tanto el carrito como las órdenes** a partir de ese momento.

Si el nombre ya existía, se reconoce (no se duplica) y se recupera su carrito real desde
Basket.API. Si el guardado en base de datos falla, no se cambia de usuario (se muestra un error
junto al input) — la identidad solo cambia cuando quedó confirmada en el backend.

## Bugs corregidos en Basket (antes de agregar Orders)

1. **Postgres roto**: `Basket.API/appsettings.json` tenía `Port:5433` (dos puntos en vez de `=`,
   Npgsql no lo parsea) y además apuntaba al puerto de Catalog. Corregido a `Port=5434` (el puerto
   real de `basketdb` en `docker-compose.override.yml`).
2. **docker-compose roto**: `basket.api` apuntaba a `src/Basket/Basket.Api/Dockerfile`, que no
   existe. Corregido a `src/Basket.API/Dockerfile` (la ruta real).
3. **`.sln` roto**: el proyecto `Basket.API` apuntaba a `..\Basket.API\Basket.API.csproj`, una
   carpeta vacía fuera de `eshop-service` (solo tenía residuos de `obj/`). Corregido a
   `src\Basket.API\Basket.API.csproj`, el proyecto real.
4. **Validación muerta**: ni `Basket.API` ni ninguna otra API llamaban a
   `AddValidatorsFromAssembly`, así que `IValidator<T>` nunca se registraba en el contenedor y
   `ValidationBehavior` corría siempre con una lista vacía — los `Validator` de Basket
   (`StoreBasketCommandValidator`, `DeleteBasketCommandValidator`) existían pero jamás se
   ejecutaban. Se agregó el registro en `Basket.API/Program.cs` (y en `Orders.API` desde el
   inicio).
5. `StoreBasketCommandValidator` validaba `Cart` con `NotEmpty()` pero el mensaje hablaba de
   "usuario" — nunca validaba `Cart.UserName` realmente. Corregido para validar el campo correcto.
6. Metadata de Swagger en los endpoints de Basket con copy-paste de Catalog (`WithName("GetProductById")`,
   `WithName("CreateProduct")`, resúmenes que decían "Producto"). Corregido.

Redis ya estaba bien configurado, no requirió cambios.

## Orders.API

### Responsabilidad

Recibe `POST /api/orders { customerId }`, consulta el Basket de ese cliente en Basket.API,
valida que tenga productos y datos consistentes, arma la orden **conservando el precio que
tenía cada item en el Basket** (no vuelve a consultar Catalog), calcula subtotal/impuestos/total
y la persiste en MongoDB Atlas.

### Endpoints

| Método | Ruta | Descripción |
|---|---|---|
| POST | `/api/orders` | Crea una orden desde el Basket de `customerId`. Header `Idempotency-Key` requerido. `201` la primera vez, `200` si se repite la key (misma orden, no se duplica). |
| GET | `/api/orders/{id}` | `200` con la orden, `404` si no existe. |
| GET | `/api/orders/customer/{customerId}` | Lista las órdenes del cliente (`200`, lista vacía si no tiene). |
| PATCH | `/api/orders/{id}/status` | `{ "status": "Confirmed" \| "Cancelled" }`. Transiciones permitidas: `Pending → Confirmed`, `Pending → Cancelled`. Cualquier otra combinación devuelve `400`. |
| GET | `/api/orders/{id}/pdf` | Descarga el comprobante de compra en PDF (`application/pdf`). `404` si la orden no existe. |

### Comprobante en PDF

Generado con **QuestPDF** (licencia Community, gratuita) — `src/Orders.API/Services/OrderPdfGenerator.cs`.
Incluye OrderId, cliente, fecha, estado, el detalle de items con precio al momento de la compra y
los totales. El frontend agrega un botón "Descargar PDF" en la confirmación de compra que apunta
directo a este endpoint.

> SkiaSharp (motor de renderizado de QuestPDF) necesita `libfontconfig1` en la imagen Linux; ya
> está instalado en `src/Orders.API/Dockerfile`. Si corres Orders.API fuera de Docker en Linux y
> falla al generar el PDF, instala esa librería en el host.

Swagger UI disponible en `/swagger` (solo en Orders.API; Basket/Catalog no lo tenían configurado
y no se les agregó, para no tocar su pipeline existente).

### Manejo de errores

| Caso | Status |
|---|---|
| Basket vacío o inexistente | 400 |
| Item del Basket con cantidad/precio inválido | 400 |
| Falta el header `Idempotency-Key` o `CustomerId` | 400 |
| Transición de estado inválida | 400 |
| Orden no encontrada | 404 |
| Basket.API no responde | 500 (mensaje genérico, sin detalles internos) |
| MongoDB no disponible | 500 (mensaje genérico; el error real de Mongo solo se loggea server-side, nunca se expone al cliente) |

### Idempotencia

`IdempotencyKey` es un índice único (sparse) en la colección de MongoDB. Si el `POST` se repite
con la misma key —doble clic, retry de red— no se inserta una segunda orden: se devuelve la
orden original con `200 OK`. Si dos requests con la misma key llegan en paralelo, el que pierde
la carrera contra el índice único recupera la orden del ganador en vez de fallar.

## Variables de entorno

Ninguna cadena de conexión sensible vive en el código ni en `appsettings.json` de Orders.API.

| Variable | Servicio | Ejemplo |
|---|---|---|
| `ConnectionStrings__MongoDb` | Orders.API | `mongodb+srv://user:pass@cluster.mongodb.net/OrdersDb?retryWrites=true&w=majority` |
| `Services__BasketApi` | Orders.API | `http://basket.api:8080` (docker) / `http://localhost:5271` (local) |
| `ConnectionStrings__Database` | Basket.API / Catalog.API | ya existían |
| `ConnectionStrings__Redis` | Basket.API | ya existía |

Si `ConnectionStrings__MongoDb` no está definida, Orders.API falla rápido con un mensaje claro
(`InvalidOperationException`, 500) en vez de arrancar en un estado inconsistente.

### Frontend (`eshop-vue-front/.env*`)

```
VITE_ORDERS_API_URL=http://localhost:5272   # o la URL real desplegada
```

## Cómo correr

### Con Docker Compose

```bash
export ORDERS_MONGODB_CONNECTION_STRING="mongodb+srv://usuario:password@tu-cluster.mongodb.net/OrdersDb?retryWrites=true&w=majority"
cd eshop-service
docker compose up --build
```

Expone: Catalog.API en `:8080`, Basket.API en `:8081`, Orders.API en `:8082`.

### Local (sin Docker)

```bash
# Terminal 1
cd eshop-service/src/Basket.API && dotnet run
# Terminal 2
export ConnectionStrings__MongoDb="mongodb+srv://..."
cd eshop-service/src/Orders.API && dotnet run
# Terminal 3
cd eshop-vue-front && npm run dev
```

## Despliegue en producción (Render + Netlify + MongoDB Atlas)

Catalog.API y Basket.API ya están desplegados como dos Web Services de Render apuntando al mismo
repo de GitHub (`eshop-catalog-api`), cada uno con un `Dockerfile Path` distinto. Orders.API sigue
el mismo patrón.

### 1. MongoDB Atlas

1. **Database Access** → Add New Database User: usuario/contraseña (auth nativo), rol
   `readWrite` sobre la base `OrdersDb` (alcance mínimo, no `Atlas admin`).
2. **Network Access** → Add IP Address → `0.0.0.0/0` (Allow access from anywhere). Render no
   tiene IP saliente fija en el plan free, así que hay que permitir cualquier IP a nivel de Atlas
   (la seguridad real la da el usuario/contraseña).
3. **Database** → cluster `Orders` → **Connect** → **Drivers** → copia el connection string
   (`mongodb+srv://usuario:<password>@....mongodb.net/?retryWrites=true&w=majority&appName=...`)
   y reemplaza `<password>` por la contraseña real.
4. Esa cadena **no se pega en appsettings.json ni se comparte por chat**: se pone directo como
   variable de entorno en Render (paso siguiente).

### 2. Orders.API en Render

1. **New +** → **Web Service** → conecta el mismo repo GitHub que ya usan Catalog/Basket.
2. Runtime: **Docker**. Dockerfile Path: `src/Orders.API/Dockerfile`. Docker Build Context:
   raíz del repo (igual que los otros dos servicios).
3. Instance type: Free está bien para probar.
4. Environment Variables:
   - `ConnectionStrings__MongoDb` = el connection string de Atlas del paso anterior.
   - `Services__BasketApi` = `https://eshop-basket-api-dnu0.onrender.com` (la URL real de
     Basket.API ya desplegado).
   - `ASPNETCORE_ENVIRONMENT` = `Production` (opcional, no cambia el comportamiento del CORS).
5. Deploy. Cuando termine, Render te da una URL pública tipo
   `https://eshop-orders-api-xxxx.onrender.com` — pruébala con `GET /health` y `GET /swagger`.

### 3. Frontend (Netlify)

Actualiza `VITE_ORDERS_API_URL` en `eshop-vue-front/.env.production` con la URL real que dio
Render, haz commit/push — Netlify redespliega solo si el auto-deploy está activo.

## Pruebas manuales

```bash
ORDERS=http://localhost:5272

# P1 - Crear orden válida (201). El customerId debe tener un Basket con productos
# (agrégalo antes desde el frontend o con POST /basket en Basket.API).
curl -i -X POST $ORDERS/api/orders \
  -H "Content-Type: application/json" -H "Idempotency-Key: demo-1" \
  -d '{"customerId":"guest-demo"}'

# P2 - Consultar orden (200)
curl -i $ORDERS/api/orders/{id-devuelto-en-P1}

# P3 - Basket vacío (400)
curl -i -X POST $ORDERS/api/orders \
  -H "Content-Type: application/json" -H "Idempotency-Key: demo-3" \
  -d '{"customerId":"usuario-sin-carrito"}'

# P4 - Repetir Idempotency-Key (misma orden, 200, no duplica)
curl -i -X POST $ORDERS/api/orders \
  -H "Content-Type: application/json" -H "Idempotency-Key: demo-1" \
  -d '{"customerId":"guest-demo"}'

# P5 - Pending -> Confirmed (200)
curl -i -X PATCH $ORDERS/api/orders/{id}/status \
  -H "Content-Type: application/json" -d '{"status":"Confirmed"}'

# P6 - Transición inválida, ej. Confirmed -> Cancelled (400)
curl -i -X PATCH $ORDERS/api/orders/{id}/status \
  -H "Content-Type: application/json" -d '{"status":"Cancelled"}'

# P7 - MongoDB no disponible: sin ConnectionStrings__MongoDb definida, cualquier
# request a Orders.API responde 500 con un mensaje genérico (ver arriba), sin stack trace.

# P8 - Flujo Vue: agregar productos al carrito → abrir el carrito → "Proceder al pago →"
# → el drawer muestra OrderId, fecha, estado y total de la orden creada.
```

## Frontend: flujo "Realizar compra"

En `CartView.vue`, el botón **"Proceder al pago →"** llama a `POST /api/orders` con el
`userName` del carrito como `customerId` y un `Idempotency-Key` nuevo (`crypto.randomUUID()`)
por cada intento. Si la orden se crea, el drawer del carrito cambia a una vista de confirmación
(OrderId, fecha, estado, total) y el carrito se vacía tanto local como en Basket.API. Si falla,
se muestra un mensaje de error dentro del propio drawer sin cerrar el flujo.
