"use client";

import { useEffect, useState } from "react";
import { Panel, Shell } from "@/app/components";
import { api } from "@/lib/api";

type Parameter = { id: string; group: string; key: string; value: string; description: string };
export default function ParametersPage() {
  const [items, setItems] = useState<Parameter[]>([]); const [message, setMessage] = useState("");
  useEffect(() => { api<Parameter[]>("/api/administration/parameters").then(setItems).catch(e => setMessage(e.message)); }, []);
  async function save(item: Parameter) { try { await api(`/api/administration/parameters/${item.id}`, { method: "PUT", body: JSON.stringify({ value: item.value }) }); setMessage("Parametrización guardada y auditada."); } catch (e) { setMessage(e instanceof Error ? e.message : "No se pudo guardar."); } }
  return <Shell title="Parametrizaciones" subtitle="Reglas operativas editables sin cambiar código.">{message && <p className={message.includes("guardada") ? "success banner" : "error banner"}>{message}</p>}<Panel title="Configuración general"><div className="parameter-list">{items.map((item, index) => <div key={item.id}><div><span className="eyebrow">{item.group}</span><strong>{item.description}</strong><small>{item.key}</small></div><input value={item.value} onChange={event => setItems(current => current.map((x, i) => i === index ? { ...x, value: event.target.value } : x))}/><button className="button ghost" onClick={() => save(item)}>Guardar</button></div>)}</div></Panel></Shell>;
}
