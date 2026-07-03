# Despliegues automáticos

## Mapeo de ramas

| Rama | GitHub Environment | Archivo preferido |
|---|---|---|
| `dev` | `development` | `deploy/docker-compose.dev.yml` |
| `test` | `testing` | `deploy/docker-compose.test.yml` |
| `prod` | `production` | `deploy/docker-compose.prod.yml` |

Cada `push` ejecuta validaciones y el workflow de despliegue. Mientras no exista aplicación o servidor, el despliegue permanece deshabilitado mediante `DEPLOY_ENABLED`.

## Configuración por ambiente

Crear en GitHub los environments `development`, `testing` y `production`. En cada uno configurar:

### Secrets

| Nombre | Uso |
|---|---|
| `DEPLOY_HOST` | Host o IP del servidor. |
| `DEPLOY_USER` | Usuario SSH sin privilegios interactivos innecesarios. |
| `DEPLOY_SSH_PRIVATE_KEY` | Clave privada dedicada al despliegue. |

### Variables

| Nombre | Uso |
|---|---|
| `DEPLOY_ENABLED` | Cambiar a `true` para activar el despliegue. |
| `DEPLOY_PATH` | Ruta absoluta del clon en el servidor. |
| `ENVIRONMENT_URL` | URL mostrada por GitHub en el deployment. |
| `HEALTHCHECK_URL` | Endpoint opcional que debe responder correctamente. |

## Preparación del servidor

1. Instalar Git, Docker Engine y Docker Compose.
2. Crear un usuario de despliegue con acceso limitado a Docker y al directorio objetivo.
3. Clonar el repositorio en `DEPLOY_PATH` y configurar una deploy key de solo lectura.
4. Crear el archivo Compose del ambiente y sus secretos fuera del repositorio.
5. Ejecutar manualmente el primer arranque y validar el health check.
6. Activar `DEPLOY_ENABLED=true`.

## Protecciones recomendadas

- `development`: despliegue automático desde `dev`.
- `testing`: despliegue automático desde `test`, después de CI exitoso.
- `production`: restringir a la rama `prod` y exigir aprobación manual en el GitHub Environment.
- Proteger `test` y `prod` contra push directo y exigir pull request.
- No guardar credenciales, `.env` ni certificados en Git.

## Comportamiento

El servidor actualiza únicamente mediante `git pull --ff-only`; si existen cambios locales o la historia diverge, el despliegue se detiene sin sobrescribir archivos. Luego actualiza las imágenes, levanta Compose y ejecuta el health check configurado.

