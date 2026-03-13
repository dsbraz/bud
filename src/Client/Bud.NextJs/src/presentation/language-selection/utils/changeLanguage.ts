"use server";

import { cookies } from "next/headers";

export async function changeLanguage(language: string) {
  const store = await cookies();
  store.set("locale", language);
}
