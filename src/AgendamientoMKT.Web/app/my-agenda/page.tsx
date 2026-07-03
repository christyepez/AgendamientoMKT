"use client";

import { Panel, Shell } from "@/app/components";

const hours = ["08:30", "09:30", "10:30", "11:30", "12:30", "13:30", "14:30", "15:30", "16:30"];
export default function AgendaPage() {
  return <Shell title="Mi agenda" subtitle="Tu semana, compromisos y capacidad disponible."><div className="toolbar"><button className="button ghost">‹ Semana anterior</button><strong>6–10 julio 2026</strong><button className="button ghost">Semana siguiente ›</button></div><Panel title="Semana laboral"><div className="calendar-grid"><div className="calendar-head"/><>{["Lun 6","Mar 7","Mié 8","Jue 9","Vie 10"].map(day => <div className="calendar-head" key={day}>{day}</div>)}</>{hours.flatMap((hour, index) => [<div className="calendar-time" key={`${hour}-time`}>{hour}</div>, ...[0,1,2,3,4].map(day => <div className="calendar-cell" key={`${hour}-${day}`}>{index === 1 && day === 0 && <span className="event">Diseño campaña<br/><small>09:30–11:30</small></span>}{index === 5 && day === 2 && <span className="event teal">Planificación redes<br/><small>13:30–15:30</small></span>}</div>)])}</div></Panel></Shell>;
}
