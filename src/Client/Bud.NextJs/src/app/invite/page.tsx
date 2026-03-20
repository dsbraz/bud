import { redirect } from "next/navigation";

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
    return (
      <InvalidInviteMessage message="Link de convite inválido ou expirado." />
    );
  }

  const baseUrl = process.env.NEXT_PUBLIC_APP_URL;

  redirect(
    `${baseUrl}/api/o/invite?token=${encodeURIComponent(token)}&email=${encodeURIComponent(email)}`,
  );
}

function InvalidInviteMessage({ message }: { message: string }) {
  return (
    <div className="flex h-screen items-center justify-center bg-gray-50">
      <h1 className="text-xl font-bold text-red-600">{message}</h1>
    </div>
  );
}
