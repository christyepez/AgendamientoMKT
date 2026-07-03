export const API_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5200";

export type UserProfile = { id: string; email: string; displayName: string; siteId: string; roles: string[]; permissions: string[] };
export type LoginResponse = { token: string; expiresAt: string; user: UserProfile };
export type MenuItem = { code: string; label: string; route: string; order: number; requiredPermission: string };

export function session(): LoginResponse | null {
  if (typeof window === "undefined") return null;
  const raw = localStorage.getItem("mkt-session");
  return raw ? (JSON.parse(raw) as LoginResponse) : null;
}

export async function api<T>(path: string, init?: RequestInit): Promise<T> {
  const current = session();
  const response = await fetch(`${API_URL}${path}`, {
    ...init,
    headers: { "Content-Type": "application/json", ...(current ? { Authorization: `Bearer ${current.token}` } : {}), ...init?.headers },
    cache: "no-store",
  });
  if (response.status === 401 && typeof window !== "undefined") { localStorage.removeItem("mkt-session"); window.location.href = "/"; }
  if (!response.ok) {
    const problem = (await response.json().catch(() => null)) as { title?: string; message?: string } | null;
    throw new Error(problem?.title ?? problem?.message ?? `Error ${response.status}`);
  }
  if (response.status === 204) return undefined as T;
  return (await response.json()) as T;
}
