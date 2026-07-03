"use client";

import { useEffect, useState } from "react";
import { Panel, Shell } from "@/app/components";
import { api } from "@/lib/api";

type Audit = { id: number; occurredAt: string; actorId?: string; action: string; entityType: string; entityId: string; dataJson: string };
export default function AuditPage() {
  const [items, setItems] = useState<Audit[]>([]); useEffect(() => { api<Audit[]>("/api/administration/audit?take=200").then(setItems); }, []);
  return <Shell title="Auditoría" subtitle="Trazabilidad permanente de cambios y decisiones."><Panel title="Eventos recientes"><div className="table-wrap"><table><thead><tr><th>Fecha</th><th>Acción</th><th>Entidad</th><th>Actor</th><th>Detalle</th></tr></thead><tbody>{items.map(x => <tr key={x.id}><td>{new Date(x.occurredAt).toLocaleString("es-EC")}</td><td><span className="tag">{x.action}</span></td><td>{x.entityType}<small>{x.entityId.slice(0, 8)}</small></td><td>{x.actorId?.slice(0,8) ?? "Sistema"}</td><td><code>{x.dataJson.slice(0,80)}</code></td></tr>)}</tbody></table></div></Panel></Shell>;
}
