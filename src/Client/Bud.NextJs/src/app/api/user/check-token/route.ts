import { NextRequest, NextResponse } from "next/server";

export async function GET(request: NextRequest) {
  const { searchParams } = request.nextUrl;
  const token = searchParams.get("token");
  const email = searchParams.get("email");

  if (!token || !email) {
    return NextResponse.json(
      { valid: false, message: "token e email são obrigatórios." },
      { status: 400 }
    );
  }

  const URL = process.env.BUD_API_URL;

  const response = await fetch(
    `${URL}/api/user/check-token?token=${encodeURIComponent(token)}&email=${encodeURIComponent(email)}`,
    {
      method: "GET",
      headers: {
        "Content-Type": "application/json",
      },
    }
  );

  const data = await response.json();

  if (!response.ok) {
    return NextResponse.json(
      { valid: false, message: data?.message ?? "Token inválido ou expirado." },
      { status: response.status }
    );
  }

  return NextResponse.json({ valid: true, ...data });
}
