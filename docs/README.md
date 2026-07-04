# Documentación

## Arquitectura

- [Diseño integral del módulo Booking](architecture/arquitectura-modulo-booking-marketing.md)
- [Stack tecnológico y estructura backend](architecture/technology-stack.md)

## Manuales

- [Manual técnico completo](technical/technical-manual.md)
- [Manual funcional con capturas](functional/functional-manual.md)

## Organización documental

| Carpeta | Contenido previsto |
|---|---|
| `architecture/` | C4, modelo de dominio, ADR y requisitos no funcionales. |
| `api/` | OpenAPI, eventos y contratos con sistemas externos. |
| `functional/` | Casos de uso, workflows, permisos y aceptación. |
| `technical/` | Implementación, APIs, datos, seguridad, operación y soporte. |
| `operations/` | Despliegue, configuración, monitoreo y recuperación. |

La documentación debe actualizarse junto con los cambios funcionales y técnicos correspondientes.

## Operación

- [Despliegues automáticos](operations/automatic-deployments.md)
- [Secretos cifrados con AES-256-GCM](operations/encrypted-secrets.md)
