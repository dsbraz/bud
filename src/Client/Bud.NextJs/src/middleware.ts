import { auth0 } from "./lib/auth0";
import { NextRequest, NextResponse } from "next/server";

export async function middleware(request: NextRequest) {
  const authResponse = await auth0.middleware(request);

  if (request.nextUrl.pathname.startsWith("/auth")) {
    return authResponse;
  }

  const session = await auth0.getSession(request);

  if (!session) {
    const loginUrl = new URL("/auth/login", request.url);
    loginUrl.searchParams.set("returnTo", request.nextUrl.pathname);
    return NextResponse.redirect(loginUrl);
  }

  return authResponse;
}

export const config = {
  matcher: ["/((?!_next/static|_next/image|favicon.ico|.*\\..*).*)"],
};
