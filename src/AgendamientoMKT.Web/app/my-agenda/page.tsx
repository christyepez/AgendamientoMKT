"use client";

import { useState } from "react";
import { Panel, Shell } from "@/app/components";

const hours = ["08:30", "09:30", "10:30", "11:30", "12:30", "13:30", "14:30", "15:30", "16:30"];

export default function AgendaPage() {
  const [view, setView] = useState<"week" | "list">("week");
  return <Shell title="Mi agenda" subtitle="Tu semana, compromisos y capacidad disponible." action={<div className="segmented"><button className={view === "week" ? "active" : ""} onClick={() => setView("week")}>Semana</button><button className={view === "list" ? "active" : ""} onClick={() => setView("list")}>Lista</button></div>}>
    <div className="toolbar agenda-toolbar"><button className="button ghost" aria-label="Semana anterior">‹</button><strong>6–10 julio 2026</strong><button className="button ghost" aria-label="Semana siguiente">›</button><span className="capacity-chip">16 h disponibles</span></div>
    {view === "week" ? <Panel title="Semana laboral"><div className="table-wrap"><div className="calendar-grid"><div className="calendar-head"/>{["Lun 6","Mar 7","Mié 8","Jue 9","Vie 10"].map(day => <div className="calendar-head" key={day}>{day}</div>)}{hours.flatMap((hour, index) => [<div className="calendar-time" key={`${hour}-time`}>{hour}</div>, ...[0,1,2,3,4].map(day => <div className="calendar-cell" key={`${hour}-${day}`}>{index === 1 && day === 0 && <span className="event">Diseño campaña<br/><small>09:30–11:30</small></span>}{index === 5 && day === 2 && <span className="event teal">Planificación redes<br/><small>13:30–15:30</small></span>}</div>)])}</div></div></Panel> : <Panel title="Próximos compromisos"><div className="agenda-list"><article><time>Lun 6 · 09:30</time><div><strong>Diseño campaña</strong><small>2 h · Diseño · Prioridad normal</small></div><span className="status">Confirmado</span></article><article><time>Mié 8 · 13:30</time><div><strong>Planificación redes</strong><small>2 h · Redes sociales</small></div><span className="status">Confirmado</span></article></div></Panel>}
  </Shell>;
}
