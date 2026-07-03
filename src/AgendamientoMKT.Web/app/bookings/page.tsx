"use client";

import { FormEvent, useEffect, useState } from "react";
import { Modal, Panel, Shell, SubmitButton } from "@/app/components";
import { api } from "@/lib/api";

type Booking = { id: string; title: string; priority: string; estimatedHours: number; status: string; version: number; assignments: unknown[] };
type Lookup = { id: string; code: string; name: string };
type Lookups = { sites: Lookup[]; services: Lookup[] };

export default function BookingsPage() {
  const [items, setItems] = useState<Booking[]>([]); const [lookups, setLookups] = useState<Lookups>({ sites: [], services: [] }); const [open, setOpen] = useState(false); const [error, setError] = useState("");
  const load = () => Promise.all([api<Booking[]>("/api/bookings"), api<Lookups>("/api/administration/lookups")]).then(([bookings, refs]) => { setItems(bookings); setLookups(refs); }).catch(e => setError(e.message));
  useEffect(() => { void load(); }, []);
  async function create(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); const data = new FormData(event.currentTarget);
    try { await api("/api/bookings", { method: "POST", body: JSON.stringify({ requirementId: data.get("requirementId"), activityId: data.get("activityId"), serviceId: data.get("serviceId"), siteId: data.get("siteId"), title: data.get("title"), priority: data.get("priority"), estimatedHours: Number(data.get("estimatedHours")) }) }); setOpen(false); await load(); } catch (e) { setError(e instanceof Error ? e.message : "No se pudo crear."); }
  }
  return <Shell title="Booking del equipo" subtitle="Planifica requerimientos y protege la capacidad disponible." action={<button className="button primary" onClick={() => setOpen(true)}>+ Nuevo booking</button>}>
    <div className="toolbar"><input placeholder="Buscar por campaña, prioridad o estado…"/><select><option>Todas las sedes</option>{lookups.sites.map(x => <option key={x.id}>{x.name}</option>)}</select><select><option>Todos los estados</option><option>Draft</option><option>Confirmed</option></select></div>
    {error && <p className="error banner">{error}</p>}<Panel title={`${items.length} bookings`}><div className="table-wrap"><table><thead><tr><th>Actividad</th><th>Prioridad</th><th>Horas</th><th>Estado</th><th>Equipo</th><th>Versión</th></tr></thead><tbody>{items.map(x => <tr key={x.id}><td><strong>{x.title}</strong><small>{x.id.slice(0,8)}</small></td><td><span className={`tag ${x.priority.toLowerCase()}`}>{x.priority}</span></td><td>{x.estimatedHours} h</td><td><span className="status">{x.status}</span></td><td>{x.assignments.length}</td><td>v{x.version}</td></tr>)}{!items.length && <tr><td colSpan={6} className="empty">Aún no hay bookings. Crea el primero desde un requerimiento.</td></tr>}</tbody></table></div></Panel>
    {open && <Modal title="Nuevo booking" onClose={() => setOpen(false)}><form className="form-grid" onSubmit={create}><label className="wide">Nombre de actividad<input name="title" required /></label><label>ID requerimiento<input name="requirementId" type="text" required /></label><label>ID actividad<input name="activityId" type="text" required /></label><label>Servicio<select name="serviceId" required>{lookups.services.map(x => <option value={x.id} key={x.id}>{x.name}</option>)}</select></label><label>Sede<select name="siteId" required>{lookups.sites.map(x => <option value={x.id} key={x.id}>{x.name}</option>)}</select></label><label>Prioridad<select name="priority"><option>Normal</option><option>High</option><option>Urgent</option></select></label><label>Horas estimadas<input name="estimatedHours" type="number" min="1" defaultValue="8" /></label><footer className="wide"><button type="button" className="button ghost" onClick={() => setOpen(false)}>Cancelar</button><SubmitButton>Crear booking</SubmitButton></footer></form></Modal>}
  </Shell>;
}
