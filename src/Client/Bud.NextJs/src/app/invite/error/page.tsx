interface InviteErrorPageProps {
  searchParams: Promise<{ message?: string }>;
}

export default async function InviteErrorPage({
  searchParams,
}: InviteErrorPageProps) {
  const params = await searchParams;
  const message = params?.message ?? "Link de convite inválido ou expirado.";

  return (
    <div className="flex h-screen items-center justify-center bg-gray-50">
      <h1 className="text-xl font-bold text-red-600">{message}</h1>
    </div>
  );
}
