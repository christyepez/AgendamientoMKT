# Integraciones Microsoft 365

## Configuración segura

Las credenciales se guardan exclusivamente dentro del archivo cifrado AES-256-GCM de cada ambiente:

```json
{
  "MicrosoftGraph": {
    "TenantId": "tenant-id",
    "ClientId": "application-id",
    "ClientSecret": "secret-value"
  }
}
```

La aplicación registrada en Microsoft Entra ID debe usar permisos de aplicación de mínimo privilegio. Los permisos para calendarios, Teams o Planner se habilitarán progresivamente cuando se implemente cada sincronización.

## Comprobación

Un administrador puede abrir **Centro de configuración > Integraciones** y ejecutar **Probar conexión**. El backend solicita un token mediante OAuth 2.0 `client_credentials`; nunca devuelve el token ni las claves al navegador.

- `GET /api/administration/integrations`: informa si las credenciales están configuradas.
- `POST /api/administration/integrations/test`: verifica autenticación con Microsoft Entra ID.

Esta fase establece autenticación y observabilidad. La creación de eventos de Outlook, mensajes de Teams y tareas de Planner se activará después de aprobar los permisos institucionales y definir las cuentas técnicas.
