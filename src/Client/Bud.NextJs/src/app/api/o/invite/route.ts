import { NextRequest, NextResponse } from "next/server";

export async function GET(request: NextRequest) {
  const { searchParams } = request.nextUrl;
  const token = searchParams.get("token");
  const email = searchParams.get("email");

  const errorRedirect = (message: string) =>
    NextResponse.redirect(
      new URL(
        `/invite/error?message=${encodeURIComponent(message)}`,
        request.nextUrl.origin,
      ),
    );

  if (!token) {
    return errorRedirect("Link de convite inválido.");
  }

  const apiUrl = process.env.BUD_API_URL;

  const response = await fetch(
    `${apiUrl}/api/user/check-token?token=${encodeURIComponent(token)}`,
    {
      method: "GET",
      headers: {
        "Content-Type": "application/json",
      },
    },
  );

  if (!response.ok) {
    return errorRedirect("Token de convite inválido ou expirado.");
  }

  // Se email foi fornecido, setar cookie e redirecionar para login
  if (email) {
    const redirectUrl = new URL(
      `/auth/login?login_hint=${encodeURIComponent(email)}&screen_hint=signup`,
      request.nextUrl.origin,
    );

    const res = NextResponse.redirect(redirectUrl);
    res.cookies.set({
      name: "invite_token",
      value: token,
      httpOnly: true,
      secure: process.env.NODE_ENV === "production",
      sameSite: "lax",
      maxAge: 60 * 60 * 2,
      path: "/",
    });

    return res;
  }

  return NextResponse.json({ valid: true });
}
