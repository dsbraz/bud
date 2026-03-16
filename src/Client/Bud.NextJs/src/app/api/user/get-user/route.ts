import { auth0 } from "@/lib/auth0";
import { NextResponse } from "next/server";

export async function GET() {
  const URL = process.env.BUD_API_URL;
  const { token: accessToken } = await auth0.getAccessToken();

  console.log(accessToken);
  const response = await fetch(`${URL}/api/user/get-user`, {
    headers: {
      Authorization: `Bearer ${accessToken}`,
      "Content-Type": "application/json",
    },
  });

  const data = await response;
  console.log(data);
  return NextResponse.json(data);
}
