import type { Metadata } from "next";
import "./globals.css";
import "./configuration.css";

export const metadata: Metadata = { title: "Agendamiento MKT", description: "Planificación y capacidad del equipo de Marketing" };

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return <html lang="es"><body>{children}</body></html>;
}
