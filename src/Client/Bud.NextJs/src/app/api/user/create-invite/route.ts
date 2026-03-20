import { auth0 } from "@/lib/auth0";
import { getBudToken } from "@/lib/bud-token";
import { NextResponse } from "next/server";

export async function POST(request: Request) {
  // Front → Front: verifica sessão Auth0
  const session = await auth0.getSession();
  if (!session) {
    return NextResponse.json({ error: "Unauthorized" }, { status: 401 });
  }

  // Front → Back: usa bud_token via cookie
  const apiUrl = process.env.BUD_API_URL;
  const token = await getBudToken();
  const body = await request.json();

  const response = await fetch(`${apiUrl}/api/user/user-invite`, {
    method: "POST",
    body: JSON.stringify(body),
    headers: {
      Authorization: `Bearer ${token}`,
      "Content-Type": "application/json",
    },
  });

  const data = await response.json();
  return NextResponse.json(data);
}
