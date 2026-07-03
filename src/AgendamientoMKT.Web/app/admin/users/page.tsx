"use client";

import { FormEvent, useEffect, useState } from "react";
import { Modal, Panel, Shell, SubmitButton } from "@/app/components";
import { api } from "@/lib/api";

type User = { id: string; email: string; displayName: string; siteId: string; isActive: boolean; roles: string[] };
type Lookup = { id: string; code: string; name: string };
type Lookups = { roles: Lookup[]; sites: Lookup[] };

export default function UsersPage() {
  const [users, setUsers] = useState<User[]>([]); const [refs, setRefs] = useState<Lookups>({ roles: [], sites: [] }); const [open, setOpen] = useState(false); const [message, setMessage] = useState("");
  const load = () => Promise.all([api<User[]>("/api/administration/users"), api<Lookups>("/api/administration/lookups")]).then(([u, r]) => { setUsers(u); setRefs(r); }).catch(e => setMessage(e.message));
  useEffect(() => { void load(); }, []);
  async function create(event: FormEvent<HTMLFormElement>) { event.preventDefault(); const data = new FormData(event.currentTarget); try { await api("/api/administration/users", { method: "POST", body: JSON.stringify({ email: data.get("email"), displayName: data.get("displayName"), password: data.get("password"), siteId: data.get("siteId"), roleIds: [data.get("roleId")] }) }); setOpen(false); await load(); } catch (e) { setMessage(e instanceof Error ? e.message : "No se pudo crear el usuario."); } }
  return <Shell title="Usuarios" subtitle="Identidad, sede y roles de acceso." action={<button className="button primary" onClick={() => setOpen(true)}>+ Nuevo usuario</button>}><div className="toolbar"><input placeholder="Buscar usuario…"/><select><option>Todos los roles</option>{refs.roles.map(x => <option key={x.id}>{x.name}</option>)}</select></div>{message && <p className="error banner">{message}</p>}<Panel title={`${users.length} usuarios activos`}><div className="table-wrap"><table><thead><tr><th>Usuario</th><th>Correo</th><th>Roles</th><th>Estado</th></tr></thead><tbody>{users.map(x => <tr key={x.id}><td><strong>{x.displayName}</strong></td><td>{x.email}</td><td>{x.roles.map(r => <span className="tag" key={r}>{r}</span>)}</td><td><span className="status">{x.isActive ? "Activo" : "Inactivo"}</span></td></tr>)}</tbody></table></div></Panel>{open && <Modal title="Nuevo usuario" onClose={() => setOpen(false)}><form className="form-grid" onSubmit={create}><label className="wide">Nombre completo<input name="displayName" required /></label><label className="wide">Correo<input name="email" type="email" required /></label><label>Contraseña inicial<input name="password" type="password" minLength={10} required /></label><label>Sede<select name="siteId">{refs.sites.map(x => <option key={x.id} value={x.id}>{x.name}</option>)}</select></label><label className="wide">Rol<select name="roleId">{refs.roles.map(x => <option key={x.id} value={x.id}>{x.name}</option>)}</select></label><footer className="wide"><button type="button" className="button ghost" onClick={() => setOpen(false)}>Cancelar</button><SubmitButton>Crear usuario</SubmitButton></footer></form></Modal>}</Shell>;
}
