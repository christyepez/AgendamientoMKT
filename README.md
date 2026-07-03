# AgendamientoMKT

Módulo integrado de booking para la Plataforma de Requerimientos de Marketing.

El proyecto permitirá planificar la capacidad del equipo, asignar integrantes a actividades, reservar bloques horarios, controlar replanificaciones y consultar disponibilidad por sede y servicio.

## Estado

MVP ejecutable en desarrollo: API, frontend, autenticación, permisos, administración, booking, auditoría, métricas y entorno Docker local.

## Documentación

- [Arquitectura del módulo Booking](docs/architecture/arquitectura-modulo-booking-marketing.md)
- [Stack tecnológico y estructura backend](docs/architecture/technology-stack.md)
- [Índice de documentación](docs/README.md)
- [Despliegues automáticos](docs/operations/automatic-deployments.md)

## Estructura prevista

```text
AgendamientoMKT/
├── docs/
│   ├── architecture/       Arquitectura C4, dominio y decisiones
│   ├── api/                Contratos y especificaciones de integración
│   ├── functional/         Reglas, flujos y criterios de aceptación
│   └── operations/         Despliegue, observabilidad y soporte
├── src/                    Código fuente futuro
├── tests/                  Pruebas futuras
└── README.md
```

## Alcance inicial

- 18 integrantes distribuidos en tres sedes.
- Jornada de lunes a viernes, 08:30–17:30.
- Booking originado exclusivamente desde requerimientos y actividades.
- Calendarios, agenda, Kanban, línea de tiempo y carga por persona.
- Integraciones previstas con Outlook, Teams, Planner, Power BI y Power Automate.
- Acceso interno y consulta pública de disponibilidad agregada.
- Auditoría y versiones permanentes.

## Base de datos local

```powershell
Copy-Item .env.example .env
docker compose run --rm sqlserver-init
```

Docker Compose reutiliza `requirements-sqlserver` y crea la base `AgendamientoMKT` de forma idempotente, sin levantar una segunda instancia. Después levanta API, web y Nginx.

Accesos locales:

- Aplicación: `http://localhost:8088`
- API/Swagger en desarrollo: `http://localhost:5200/swagger`
- Frontend directo: `http://localhost:3001`

Usuario inicial local:

- Correo: `admin@agendamientomkt.local`
- Contraseña: la configurada en `ADMIN_PASSWORD`.

Las credenciales del archivo de ejemplo deben reemplazarse antes de almacenar información real.

## Calidad

```powershell
dotnet build AgendamientoMKT.slnx -c Release
dotnet test AgendamientoMKT.slnx -c Release
cd src/AgendamientoMKT.Web
pnpm lint
pnpm typecheck
pnpm build
```
