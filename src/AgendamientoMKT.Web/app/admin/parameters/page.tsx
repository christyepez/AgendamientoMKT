"use client";

import { useEffect, useMemo, useState } from "react";
import { Panel, Shell } from "@/app/components";
import { api } from "@/lib/api";

type Parameter = { id: string; group: string; key: string; value: string; description: string };
type Lookup = { id: string; code: string; name: string };
type Role = { id: string; code: string; name: string; isActive: boolean; permissions: string[] };
type Screen = { id: string; code: string; name: string; route: string; isActive: boolean };
type Menu = { code: string; label: string; route: string; order: number; requiredPermission: string };
type Integration = { code: string; name: string; configured: boolean; available: boolean; status: string; checkedAt: string };
type Center = { parameters: Parameter[]; sites: Lookup[]; services: Lookup[]; roles: Role[]; screens: Screen[]; menu: Menu[] };
type Tab = "general" | "catalogs" | "security" | "navigation" | "integrations";

const empty: Center = { parameters: [], sites: [], services: [], roles: [], screens: [], menu: [] };
const tabLabels: Record<Tab, string> = { general: "Operación", catalogs: "Catálogos", security: "Seguridad y acceso", navigation: "Pantallas y menú", integrations: "Integraciones" };

export default function ParametersPage() {
  const [data, setData] = useState<Center>(empty); const [tab, setTab] = useState<Tab>("general"); const [message, setMessage] = useState(""); const [query, setQuery] = useState(""); const [integrations, setIntegrations] = useState<Integration[]>([]); const [testing, setTesting] = useState(false);
  useEffect(() => { api<Center>("/api/administration/configuration-center").then(setData).catch(e => setMessage(e.message)); }, []);
  const groups = useMemo(() => Object.groupBy(data.parameters, item => item.group), [data.parameters]);
  async function save(item: Parameter) { try { await api(`/api/administration/parameters/${item.id}`, { method: "PUT", body: JSON.stringify({ value: item.value }) }); setMessage(`“${item.description}” guardado y auditado.`); } catch (e) { setMessage(e instanceof Error ? e.message : "No se pudo guardar."); } }
  const update = (id: string, value: string) => setData(current => ({ ...current, parameters: current.parameters.map(item => item.id === id ? { ...item, value } : item) }));
  const matches = (value: string) => value.toLowerCase().includes(query.toLowerCase());
  async function loadIntegrations(test = false) { setTesting(test); try { const result = await api<Integration[]>(`/api/administration/integrations${test ? "/test" : ""}`, { method: test ? "POST" : "GET" }); setIntegrations(result); if (test) setMessage("Prueba de integración finalizada."); } catch (e) { setMessage(e instanceof Error ? e.message : "No se pudo verificar la integración."); } finally { setTesting(false); } }

  return <Shell title="Centro de configuración" subtitle="Todas las parametrizaciones funcionales, de acceso e integración en un solo lugar.">
    <section className="configuration-summary"><div><strong>{data.parameters.length}</strong><span>Parámetros</span></div><div><strong>{data.services.length}</strong><span>Servicios</span></div><div><strong>{data.roles.length}</strong><span>Roles</span></div><div><strong>{data.screens.length}</strong><span>Pantallas</span></div><div><strong>{data.menu.length}</strong><span>Menús</span></div></section>
    <div className="config-toolbar"><nav>{(Object.keys(tabLabels) as Tab[]).map(key => <button className={tab === key ? "active" : ""} onClick={() => setTab(key)} key={key}>{tabLabels[key]}</button>)}</nav><input value={query} onChange={event => setQuery(event.target.value)} placeholder="Buscar configuración…" /></div>
    {message && <p className={message.includes("guardado") ? "success banner" : "error banner"}>{message}</p>}
    {tab === "general" && <div className="config-columns">{Object.entries(groups).filter(([group]) => !["INTEGRATIONS","NOTIFICATIONS"].includes(group)).map(([group, items]) => <Panel title={group.replaceAll("_", " ")} key={group}><div className="parameter-list">{items?.filter(item => matches(`${item.description} ${item.key}`)).map(item => <div key={item.id}><div><strong>{item.description}</strong><small>{item.key}</small></div><input value={item.value} onChange={event => update(item.id, event.target.value)} /><button className="button ghost" onClick={() => save(item)}>Guardar</button></div>)}</div></Panel>)}</div>}
    {tab === "catalogs" && <div className="config-columns two"><Panel title="Sedes"><ConfigTable headers={["Código","Nombre","Estado"]} rows={data.sites.filter(x => matches(`${x.code} ${x.name}`)).map(x => [x.code,x.name,"Activo"])} /></Panel><Panel title="Servicios de Marketing"><ConfigTable headers={["Código","Servicio","Estado"]} rows={data.services.filter(x => matches(`${x.code} ${x.name}`)).map(x => [x.code,x.name,"Activo"])} /></Panel></div>}
    {tab === "security" && <Panel title="Roles y permisos"><div className="role-grid">{data.roles.filter(x => matches(`${x.code} ${x.name} ${x.permissions.join(" ")}`)).map(role => <article key={role.id}><header><div><strong>{role.name}</strong><small>{role.code}</small></div><span className="status">{role.isActive ? "Activo" : "Inactivo"}</span></header><div>{role.permissions.length ? role.permissions.map(permission => <span className="tag" key={permission}>{permission}</span>) : <small>Sin permisos asignados</small>}</div></article>)}</div></Panel>}
    {tab === "navigation" && <div className="config-columns two"><Panel title="Pantallas"><ConfigTable headers={["Código","Pantalla","Ruta","Estado"]} rows={data.screens.filter(x => matches(`${x.code} ${x.name} ${x.route}`)).map(x => [x.code,x.name,x.route,x.isActive ? "Activa" : "Inactiva"])} /></Panel><Panel title="Menú parametrizado"><ConfigTable headers={["Orden","Opción","Ruta","Permiso"]} rows={data.menu.filter(x => matches(`${x.code} ${x.label} ${x.route}`)).map(x => [String(x.order),x.label,x.route,x.requiredPermission])} /></Panel></div>}
    {tab === "integrations" && <div className="config-columns two"><Panel title="Microsoft 365 y Power Platform"><div className="integration-actions"><button className="button ghost" onClick={() => loadIntegrations(false)}>Actualizar estado</button><button className="button primary" disabled={testing} onClick={() => loadIntegrations(true)}>{testing ? "Verificando…" : "Probar conexión"}</button></div><div className="integration-list">{(integrations.length ? integrations : ["Microsoft Graph","Outlook","Teams","Planner","Power BI","Power Automate"].map((name, index) => ({ code: String(index), name, configured: false, available: false, status: "Consultar estado", checkedAt: "" }))).map(item => <div key={item.code}><span className="integration-icon">↗</span><div><strong>{item.name}</strong><small>{item.status}</small></div><span className={`tag ${item.available ? "" : "high"}`}>{item.available ? "Disponible" : item.configured ? "Configurado" : "Por configurar"}</span></div>)}</div></Panel><Panel title="Notificaciones y acceso público"><div className="parameter-list">{data.parameters.filter(item => ["PUBLIC","NOTIFICATIONS","INTEGRATIONS"].includes(item.group)).map(item => <div key={item.id}><div><strong>{item.description}</strong><small>{item.group} · {item.key}</small></div><input value={item.value} onChange={event => update(item.id, event.target.value)} /><button className="button ghost" onClick={() => save(item)}>Guardar</button></div>)}</div><div className="empty compact">Los secretos se administran cifrados por ambiente y nunca se muestran aquí.</div></Panel></div>}
  </Shell>;
}

function ConfigTable({ headers, rows }: { headers: string[]; rows: string[][] }) {
  return <div className="table-wrap"><table><thead><tr>{headers.map(x => <th key={x}>{x}</th>)}</tr></thead><tbody>{rows.map((row, index) => <tr key={`${row[0]}-${index}`}>{row.map((cell, cellIndex) => <td key={`${cell}-${cellIndex}`}>{cellIndex === 0 ? <strong>{cell}</strong> : cell}</td>)}</tr>)}{!rows.length && <tr><td colSpan={headers.length} className="empty">Sin coincidencias.</td></tr>}</tbody></table></div>;
}
