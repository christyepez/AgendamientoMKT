"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { FormEvent, ReactNode, useEffect, useState } from "react";
import { api, MenuItem, session } from "@/lib/api";

const fallbackMenu: MenuItem[] = [
  { code: "DASHBOARD", label: "Dashboard", route: "/dashboard", order: 1, requiredPermission: "DASHBOARD.VIEW" },
  { code: "BOOKINGS", label: "Booking", route: "/bookings", order: 2, requiredPermission: "BOOKING.VIEW" },
  { code: "AGENDA", label: "Mi agenda", route: "/my-agenda", order: 3, requiredPermission: "AGENDA.VIEW" },
];

export function Shell({ title, subtitle, children, action }: { title: string; subtitle: string; children: ReactNode; action?: ReactNode }) {
  const path = usePathname(); const router = useRouter(); const [menu, setMenu] = useState(fallbackMenu); const [ready, setReady] = useState(false);
  useEffect(() => {
    if (!session()) { router.replace("/"); return; }
    api<MenuItem[]>("/api/administration/menu").then(setMenu).catch(() => setMenu(fallbackMenu)).finally(() => setReady(true));
  }, [router]);
  if (!ready) return <main className="center"><div className="loader" /><p>Cargando espacio de trabajo…</p></main>;
  const user = session()?.user;
  const initials = user?.displayName?.split(" ").map(x => x[0]).slice(0, 2).join("").toUpperCase() || "U";
  return <div className="app-shell">
    <aside className="sidebar">
      <div className="brand"><span className="brand-mark">M</span><div><strong>Marketing</strong><small>Booking & capacidad</small></div></div>
      <nav>{menu.map(item => <Link className={path === item.route ? "nav-link active" : "nav-link"} href={item.route} key={item.code}><span>{icons[item.code] ?? "•"}</span>{item.label}</Link>)}</nav>
      <div className="profile"><span className="avatar">{initials}</span><div><strong>{user?.displayName}</strong><small>{user?.roles.join(", ")}</small></div><button title="Cerrar sesión" aria-label="Cerrar sesión" onClick={() => { localStorage.removeItem("mkt-session"); router.push("/"); }}>↪</button></div>
    </aside>
    <main className="workspace"><header className="topbar"><div><p className="eyebrow">Operación de Marketing</p><h1>{title}</h1><p>{subtitle}</p></div><div className="topbar-actions">{action}</div></header>{children}</main>
  </div>;
}

const icons: Record<string, string> = { DASHBOARD: "⌂", BOOKINGS: "▦", AGENDA: "◷", USERS: "♙", PARAMETERS: "⚙", AUDIT: "≡", METRICS: "↗" };

export function Stat({ label, value, note, tone = "blue" }: { label: string; value: string; note: string; tone?: string }) {
  return <article className={`stat ${tone}`}><span>{label}</span><strong>{value}</strong><small>{note}</small></article>;
}

export function Panel({ title, children, className = "" }: { title: string; children: ReactNode; className?: string }) {
  return <section className={`panel ${className}`}><div className="panel-title"><h2>{title}</h2></div>{children}</section>;
}

export function EmptyState({ title, detail, action }: { title: string; detail: string; action?: ReactNode }) {
  return <div className="empty-state"><span aria-hidden="true">◇</span><strong>{title}</strong><p>{detail}</p>{action}</div>;
}

export function Modal({ title, children, onClose }: { title: string; children: ReactNode; onClose: () => void }) {
  return <div className="modal-backdrop" role="dialog" aria-modal="true"><div className="modal"><header><h2>{title}</h2><button onClick={onClose}>×</button></header>{children}</div></div>;
}

export function SubmitButton({ children }: { children: ReactNode }) {
  const [busy, setBusy] = useState(false);
  return <button className="button primary" type="submit" disabled={busy} onClick={() => setBusy(true)}>{busy ? "Guardando…" : children}</button>;
}

export type FormHandler = (event: FormEvent<HTMLFormElement>) => Promise<void>;
