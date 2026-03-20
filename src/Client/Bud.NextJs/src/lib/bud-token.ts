import { cookies } from "next/headers";

const BUD_ADMIN_EMAIL = "admin@getbud.co";
const BUD_TOKEN_COOKIE = "bud_token";

export async function getBudToken(): Promise<string> {
  const cookieStore = await cookies();
  const existing = cookieStore.get(BUD_TOKEN_COOKIE);
  if (existing?.value) return existing.value;

  // Login direto no backend BUD — sem round-trip HTTP interno
  const apiUrl = process.env.BUD_API_URL;
  const loginResponse = await fetch(`${apiUrl}/api/sessions`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email: BUD_ADMIN_EMAIL }),
  });

  if (!loginResponse.ok) {
    throw new Error(
      `Failed to authenticate with Bud API: ${loginResponse.status}`,
    );
  }

  const session = await loginResponse.json();
  const token: string = session.token;

  cookieStore.set(BUD_TOKEN_COOKIE, token, {
    httpOnly: true,
    secure: process.env.NODE_ENV === "production",
    sameSite: "lax",
    path: "/",
  });

  return token;
}
