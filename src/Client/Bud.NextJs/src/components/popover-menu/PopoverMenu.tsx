export interface PopoverMenuItem {
  id: string;
  label: string;
  icon?: React.ElementType;
  image?: string;
  danger?: boolean;
  divider?: boolean;
  onClick?: () => void;
}

export function PopoverMenu({
  items,
  onClose,
}: {
  items: PopoverMenuItem[];
  onClose: () => void;
}) {
  return (
    <div className="flex flex-col py-1 min-w-[180px]">
      {items.map((item, i) => (
        <div key={item.id}>
          {item.divider && i > 0 && (
            <div className="my-1 border-t border-border" />
          )}
          <button
            onClick={() => {
              item.onClick?.();
              onClose();
            }}
            className={[
              "w-full flex items-center gap-2.5 px-3 py-2 text-sm rounded-md transition-colors",
              item.danger
                ? "text-destructive hover:bg-destructive/10"
                : "text-foreground hover:bg-accent",
            ].join(" ")}
          >
            {item.image && (
              <img
                src={item.image}
                alt=""
                className="h-5 w-5 rounded-full object-cover shrink-0"
              />
            )}
            {item.icon && !item.image && (
              <item.icon className="h-4 w-4 shrink-0" />
            )}
            {item.label}
          </button>
        </div>
      ))}
    </div>
  );
}
