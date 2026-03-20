import { ReactNode } from "react";

interface SubItemProps {
  active?: boolean;
  onClick?: () => void;
  children: ReactNode;
}

export function SubItem({ active, onClick, children }: SubItemProps) {
  return (
    <button
      onClick={onClick}
      className={[
        "w-full text-left px-2 py-1.5 rounded-md text-xs font-medium transition-colors duration-150",
        active
          ? "text-sidebar-accent-foreground bg-sidebar-accent"
          : "text-muted-foreground hover:text-sidebar-foreground hover:bg-sidebar-accent/60",
      ].join(" ")}
    >
      {children}
    </button>
  );
}
