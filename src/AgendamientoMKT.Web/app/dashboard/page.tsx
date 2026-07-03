"use client";

import { Panel, Shell, Stat } from "@/app/components";

export default function DashboardPage() {
  return <Shell title="Dashboard" subtitle="Capacidad, prioridades y ritmo de trabajo del equipo.">
    <section className="stats-grid"><Stat label="Ocupación semanal" value="72%" note="128 de 176 horas" /><Stat label="Disponibilidad" value="48 h" note="Próximos 7 días" tone="green" /><Stat label="Pendientes" value="6" note="Requieren planificación" tone="amber" /><Stat label="En riesgo" value="2" note="Entrega próxima" tone="red" /></section>
    <section className="content-grid"><Panel title="Carga por sede" className="span-2"><div className="bar-list">{[["Ambato",78],["Quito",64],["Online",71]].map(([name,value]) => <div key={name}><span>{name}</span><div className="bar"><i style={{ width: `${value}%` }} /></div><strong>{value}%</strong></div>)}</div></Panel><Panel title="Atención del coordinador"><ul className="attention"><li><i className="dot red"/><div><strong>2 conflictos de agenda</strong><span>Requieren replanificación</span></div></li><li><i className="dot amber"/><div><strong>3 confirmaciones pendientes</strong><span>Vencen hoy</span></div></li><li><i className="dot green"/><div><strong>Sincronización estable</strong><span>Última revisión hace 5 min</span></div></li></ul></Panel></section>
  </Shell>;
}
