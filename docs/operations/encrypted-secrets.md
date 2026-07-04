# Configuración sensible cifrada con AES-256-GCM

La aplicación carga secretos desde `config/secrets.aes256.json`. El archivo contiene un sobre JSON con versión, algoritmo, nonce, etiqueta de autenticación y texto cifrado. No contiene secretos legibles.

## Clave maestra

La variable `AGENDAMIENTO_MASTER_KEY` debe contener exactamente 32 bytes aleatorios codificados en Base64. La clave:

- No se guarda en Git.
- No se escribe dentro del archivo cifrado.
- Debe ser distinta para desarrollo, pruebas y producción.
- Se almacena como secret protegido del ambiente de despliegue.
- Debe poder rotarse siguiendo un procedimiento controlado.

## Crear el archivo cifrado

Para desarrollo local conectado al contenedor de Requirements Platform puede ejecutarse directamente:

```powershell
./scripts/Initialize-LocalSecrets.ps1
```

El script obtiene la conexión del contenedor local, genera claves aleatorias, cifra el archivo, elimina el texto claro y crea un `.env` ignorado que contiene únicamente la clave maestra y variables no sensibles.

Para otros ambientes, el procedimiento manual es:

1. Copiar `config/secrets.template.json` a un archivo temporal ignorado, por ejemplo `config/secrets.plain.json`.
2. Completar conexión SQL, JWT, credenciales de Microsoft Graph y Power Platform.
3. Generar una clave maestra de 32 bytes:

```powershell
$bytes = [Security.Cryptography.RandomNumberGenerator]::GetBytes(32)
$env:AGENDAMIENTO_MASTER_KEY = [Convert]::ToBase64String($bytes)
[Security.Cryptography.CryptographicOperations]::ZeroMemory($bytes)
```

4. Cifrar:

```powershell
./scripts/Protect-Secrets.ps1 -InputPath config/secrets.plain.json
```

5. Eliminar de forma segura el archivo de texto claro y conservar la clave en el gestor de secretos del ambiente.

## Arranque

Docker monta el archivo como solo lectura y entrega únicamente la clave maestra mediante el ambiente. La API descifra en memoria durante el inicio, añade los valores al sistema de configuración y limpia los buffers criptográficos utilizados.

Si falta el archivo, la clave no tiene 256 bits o la etiqueta GCM no coincide, la aplicación se niega a iniciar.

## Rotación

1. Recuperar el JSON desde una fuente autorizada o descifrarlo en un proceso administrativo aislado.
2. Generar una nueva clave aleatoria.
3. Volver a cifrar el archivo.
4. Actualizar el secret `AGENDAMIENTO_MASTER_KEY` y el archivo como una sola liberación.
5. Reiniciar y comprobar `/health`.

La contraseña de bootstrap de SQL Server usada por `sqlserver-init` es una excepción de infraestructura: debe provenir de un secret del ambiente y solo se necesita para crear inicialmente la base. No se registra en el archivo Compose.
