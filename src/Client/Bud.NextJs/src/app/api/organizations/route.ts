import { getBudToken } from "@/lib/bud-token";
import { NextResponse } from "next/server";

export async function GET() {
  // Front → Back: usa bud_token via cookie
  const apiUrl = process.env.BUD_API_URL;
  const token = await getBudToken();

  const response = await fetch(`${apiUrl}/api/organizations`, {
    headers: { Authorization: `Bearer ${token}` },
  });

  if (!response.ok) {
    return NextResponse.json(
      { error: "Failed to fetch organizations" },
      { status: response.status },
    );
  }

  const content = await response.json();

  return NextResponse.json(content.items);
}
