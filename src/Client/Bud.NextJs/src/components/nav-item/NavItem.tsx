import { ChevronDown, ChevronRight } from "lucide-react";
import { ReactNode, useState } from "react";

interface NavItemProps {
  icon: React.ElementType;
  label: string;
  active?: boolean;
  collapsed?: boolean;
  onClick?: () => void;
  defaultExpanded?: boolean;
  children?: ReactNode;
}

export function NavItem({
  icon: Icon,
  label,
  active,
  collapsed,
  onClick,
  defaultExpanded,
  children,
}: NavItemProps) {
  const [expanded, setExpanded] = useState(defaultExpanded ?? false);
  const hasChildren = !!children;

  const handleClick = () => {
    if (hasChildren) setExpanded((v) => !v);
    else onClick?.();
  };

  return (
    <div>
      <button
        onClick={handleClick}
        title={collapsed ? label : undefined}
        className={[
          "w-full flex items-center gap-2.5 px-2.5 py-2 rounded-lg text-sm font-medium",
          "transition-colors duration-150",
          active
            ? "bg-sidebar-accent text-sidebar-accent-foreground"
            : "text-sidebar-foreground hover:bg-sidebar-accent/60 hover:text-sidebar-accent-foreground",
        ].join(" ")}
      >
        <Icon className="h-4 w-4 shrink-0" />
        {!collapsed && (
          <>
            <span className="flex-1 text-left truncate">{label}</span>
            {hasChildren &&
              (expanded ? (
                <ChevronDown className="h-3.5 w-3.5 shrink-0 text-muted-foreground" />
              ) : (
                <ChevronRight className="h-3.5 w-3.5 shrink-0 text-muted-foreground" />
              ))}
          </>
        )}
      </button>
      {!collapsed && hasChildren && expanded && (
        <div className="mt-0.5 ml-4 pl-3 border-l border-border flex flex-col gap-0.5">
          {children}
        </div>
      )}
    </div>
  );
}
