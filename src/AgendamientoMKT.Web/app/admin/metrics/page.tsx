"use client";

import { useEffect, useState } from "react";
import { Panel, Shell, Stat } from "@/app/components";
import { api } from "@/lib/api";

type Metric = { screenCode: string; visits: number; averageDurationMs: number; errors: number };
export default function MetricsPage() {
  const [items, setItems] = useState<Metric[]>([]); useEffect(() => { api<Metric[]>("/api/administration/metrics?days=30").then(setItems); }, []); const visits = items.reduce((sum, x) => sum + x.visits, 0); const errors = items.reduce((sum, x) => sum + x.errors, 0);
  return <Shell title="Métricas de usabilidad" subtitle="Uso real, rendimiento y fricción de las pantallas."><section className="stats-grid"><Stat label="Interacciones" value={String(visits)} note="Últimos 30 días"/><Stat label="Pantallas usadas" value={String(items.length)} note="Con actividad" tone="green"/><Stat label="Errores" value={String(errors)} note="Respuestas inesperadas" tone="red"/></section><Panel title="Uso por pantalla"><div className="bar-list">{items.map(x => <div key={x.screenCode}><span>{x.screenCode}</span><div className="bar"><i style={{ width: `${Math.min(100, visits ? x.visits / visits * 100 : 0)}%` }}/></div><strong>{x.visits}</strong></div>)}</div></Panel></Shell>;
}
