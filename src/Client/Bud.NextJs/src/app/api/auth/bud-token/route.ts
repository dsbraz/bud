import { cookies } from "next/headers";
import { NextResponse } from "next/server";

const BUD_ADMIN_EMAIL = "admin@getbud.co";
const BUD_TOKEN_COOKIE = "bud_token";

export async function POST() {
  const apiUrl = process.env.BUD_API_URL;

  const response = await fetch(`${apiUrl}/api/sessions`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email: BUD_ADMIN_EMAIL }),
  });

  if (!response.ok) {
    return NextResponse.json(
      { error: "Failed to authenticate with Bud API" },
      { status: response.status },
    );
  }

  const session = await response.json();
  const token: string = session.token;

  const cookieStore = await cookies();
  cookieStore.set(BUD_TOKEN_COOKIE, token, {
    httpOnly: true,
    secure: process.env.NODE_ENV === "production",
    sameSite: "lax",
    path: "/",
  });

  return NextResponse.json({ token });
}
