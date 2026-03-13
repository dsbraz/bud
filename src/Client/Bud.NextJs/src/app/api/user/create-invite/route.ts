import { auth0 } from "@/lib/auth0";
import { NextResponse } from "next/server";

export async function POST(request: Request) {
  const URL = process.env.BUD_API_URL;
  const { token: accessToken } = await auth0.getAccessToken();
  const body = await request.json();
  console.log(body);
  const response = await fetch(`${URL}/api/user/user-invite`, {
    method: "POST",
    body: JSON.stringify(body),
    headers: {
      Authorization: `Bearer ${accessToken}`,
      "Content-Type": "application/json",
    },
  });

  const data = await response;
  console.log(data);
  return NextResponse.json(data);
}
