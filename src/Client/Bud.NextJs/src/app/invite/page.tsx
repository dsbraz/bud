import { redirect } from "next/navigation";
import { cookies } from "next/headers";

interface AcceptInviteProps {
  searchParams: Promise<{ token?: string; email?: string }>;
}

export default async function AcceptInvitePage({
  searchParams,
}: AcceptInviteProps) {
  const params = await searchParams;
  const token = params?.token;
  const email = params?.email;

  if (!token || !email) {
    return <InvalidInviteMessage message="Link de convite inválido ou expirado." />;
  }

  const baseUrl = process.env.NEXT_PUBLIC_APP_URL;

  const response = await fetch(
    `${baseUrl}/api/user/check-token?token=${encodeURIComponent(token)}&email=${encodeURIComponent(email)}`,
    { cache: "no-store" }
  );

  if (!response.ok) {
    const data = await response.json().catch(() => ({}));
    return (
      <InvalidInviteMessage
        message={data?.message ?? "Token de convite inválido ou expirado."}
      />
    );
  }

  const cookieStore = await cookies();
  cookieStore.set({
    name: "invite_token",
    value: token,
    httpOnly: true,
    secure: process.env.NODE_ENV === "production",
    sameSite: "lax",
    maxAge: 60 * 60 * 2,
    path: "/",
  });

  redirect(`/auth/login?login_hint=${encodeURIComponent(email)}&screen_hint=signup`);
}

function InvalidInviteMessage({ message }: { message: string }) {
  return (
    <div className="flex h-screen items-center justify-center bg-gray-50">
      <h1 className="text-xl font-bold text-red-600">{message}</h1>
    </div>
  );
}
