# AgendamientoMKT

Módulo integrado de booking para la Plataforma de Requerimientos de Marketing.

El proyecto permitirá planificar la capacidad del equipo, asignar integrantes a actividades, reservar bloques horarios, controlar replanificaciones y consultar disponibilidad por sede y servicio.

## Estado

MVP ejecutable en desarrollo: API, frontend, autenticación, permisos, administración, booking, auditoría, métricas y entorno Docker local.

## Documentación

- [Arquitectura del módulo Booking](docs/architecture/arquitectura-modulo-booking-marketing.md)
- [Stack tecnológico y estructura backend](docs/architecture/technology-stack.md)
- [Manual técnico completo](docs/technical/technical-manual.md)
- [Manual funcional con capturas](docs/functional/functional-manual.md)
- [Índice de documentación](docs/README.md)
- [Despliegues automáticos](docs/operations/automatic-deployments.md)
- [Secretos cifrados con AES-256-GCM](docs/operations/encrypted-secrets.md)

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
.\scripts\Initialize-LocalSecrets.ps1
docker compose up -d --build
```

El script genera una clave local, cifra la configuración y elimina el JSON temporal. Docker Compose reutiliza `requirements-sqlserver` y levanta API, web y Nginx sin incluir secretos en las imágenes.

Accesos locales:

- Aplicación: `http://localhost:8088`
- API/Swagger en desarrollo: `http://localhost:5200/swagger`
- Frontend directo: `http://localhost:3001`

Usuario inicial local:

- Correo: `admin@agendamientomkt.local`
- Contraseña: la configurada en `ADMIN_PASSWORD`.

Las credenciales del archivo de ejemplo deben reemplazarse antes de almacenar información real.

Los valores sensibles se guardan en `config/secrets.aes256.json`, cifrados y autenticados con AES-256-GCM. Consulta la guía antes de iniciar por primera vez.

## Calidad

```powershell
dotnet build AgendamientoMKT.slnx -c Release
dotnet test AgendamientoMKT.slnx -c Release
cd src/AgendamientoMKT.Web
pnpm lint
pnpm typecheck
pnpm build
```
