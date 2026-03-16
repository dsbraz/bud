"use client";

import * as React from "react";

import { CheckIcon, ChevronsUpDownIcon } from "lucide-react";

import { cn } from "@/lib/utils";
import { Button } from "@/components/ui/button";
import {
  Command,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
} from "@/components/ui/command";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover";

interface ComboboxProperties {
  values: Array<{
    value: string;
    label: string;
  }>;
  placeholder: string;
  defaultValue?: string;
  required?: boolean;
  onChange?: (value: string) => void;
}

export function ComboboxComponent({
  values,
  placeholder,
  defaultValue,
  required,
  onChange,
}: ComboboxProperties) {
  const [open, setOpen] = React.useState(false);
  const [selected, setSelected] = React.useState(defaultValue || "");

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <Button
          variant="outline"
          role="combobox"
          aria-expanded={open}
          className="w-[200px] justify-between"
        >
          {selected
            ? values.find((value) => value.value === selected)?.label
            : placeholder}
          <ChevronsUpDownIcon className="ml-2 h-4 w-4 shrink-0 opacity-50" />
        </Button>
      </PopoverTrigger>
      <PopoverContent className="w-[200px] p-0">
        <Command shouldFilter={false}>
          <CommandInput placeholder={placeholder} />
          <CommandList>
            <CommandEmpty>No framework found.</CommandEmpty>
            <CommandGroup>
              {values.map((value) => (
                <CommandItem
                  key={value.value}
                  value={value.value}
                  onSelect={(currentValue) => {
                    setSelected((selected) => {
                      if (required) {
                        return currentValue;
                      }
                      return currentValue === selected ? "" : currentValue;
                    });
                    setOpen(false);
                    if (onChange) {
                      onChange(currentValue);
                    }
                  }}
                >
                  <CheckIcon
                    className={cn(
                      "mr-2 h-4 w-4",
                      selected === value.value ? "opacity-100" : "opacity-0",
                    )}
                  />
                  {value.label}
                </CommandItem>
              ))}
            </CommandGroup>
          </CommandList>
        </Command>
      </PopoverContent>
    </Popover>
  );
}
