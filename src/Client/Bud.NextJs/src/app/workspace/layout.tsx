import { LanguageSelection } from "@/presentation/language-selection";
import { ReactNode } from "react";

interface SpecificLayoutProps {
  children: ReactNode;
}

export default function SpecificLayout({ children }: SpecificLayoutProps) {
  return (
    <div className="flex min-h-screen bg-[#FAF8F2] px-4">
      <div className="flex flex-1 flex-col">
        <LanguageSelection />
        <main className="my-auto">
          <div className="mx-auto max-w-xl">{children}</div>
        </main>
      </div>
    </div>
  );
}
