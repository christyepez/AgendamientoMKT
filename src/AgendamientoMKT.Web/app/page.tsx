"use client";

import { FormEvent, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { API_URL, LoginResponse } from "@/lib/api";

export default function LoginPage() {
  const router = useRouter(); const [error, setError] = useState(""); const [busy, setBusy] = useState(false);
  useEffect(() => { if (localStorage.getItem("mkt-session")) router.replace("/dashboard"); }, [router]);
  async function login(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); setBusy(true); setError(""); const data = new FormData(event.currentTarget);
    try {
      const response = await fetch(`${API_URL}/api/auth/login`, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ email: data.get("email"), password: data.get("password") }) });
      if (!response.ok) throw new Error("Correo o contraseña incorrectos.");
      localStorage.setItem("mkt-session", JSON.stringify(await response.json() as LoginResponse)); router.push("/dashboard");
    } catch (cause) { setError(cause instanceof Error ? cause.message : "No fue posible iniciar sesión."); setBusy(false); }
  }
  return <main className="login-page"><section className="login-story"><div className="story-content"><span className="pill">Universidad Indoamérica · Marketing</span><h1>Una agenda clara para ideas que sí llegan a tiempo.</h1><p>Planifica capacidad, protege el foco del equipo y convierte requerimientos en bloques de trabajo visibles.</p><div className="story-grid"><div><strong>18</strong><span>integrantes</span></div><div><strong>3</strong><span>sedes</span></div><div><strong>1</strong><span>agenda compartida</span></div></div></div></section>
    <section className="login-form-wrap"><form className="login-card" onSubmit={login}><span className="brand-mark large">M</span><p className="eyebrow">Agendamiento MKT</p><h2>Bienvenido de vuelta</h2><p>Ingresa con tus credenciales institucionales.</p><label>Correo institucional<input name="email" type="email" defaultValue="admin@agendamientomkt.local" required /></label><label>Contraseña<input name="password" type="password" defaultValue="Admin.ChangeMe.2026!" required /></label>{error && <p className="error">{error}</p>}<button className="button primary full" disabled={busy}>{busy ? "Ingresando…" : "Ingresar"}</button><small>Acceso seguro · Actividad auditada</small></form></section></main>;
}
