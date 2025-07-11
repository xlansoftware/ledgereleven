"use client";

import * as React from "react";
import { Check, ChevronsUpDown } from "lucide-react";

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
import { languageList } from "@/lib/language";
import { useBookStore } from "@/lib/store-book";

interface PreferredLanguageProps {
  id?: string;
  disabled?: boolean;
}

export function PreferredLanguage({ id, disabled }: PreferredLanguageProps) {
  const { getSetting, setSetting } = useBookStore();

  const value = getSetting("Language", "eng");

  const [open, setOpen] = React.useState(false);

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <Button
          id={id}
          variant="outline"
          role="combobox"
          aria-expanded={open}
          className="w-[200px] justify-between"
          disabled={disabled}
        >
          {value
            ? languageList.find((language) => language.code === value)?.name
            : "Select language..."}
          <ChevronsUpDown className="opacity-50" />
        </Button>
      </PopoverTrigger>
      <PopoverContent className="w-[200px] p-0">
        <Command>
          <CommandInput placeholder="Search language..." className="h-9" />
          <CommandList>
            <CommandEmpty>No language found.</CommandEmpty>
            <CommandGroup>
              {languageList.map((language) => (
                <CommandItem
                  key={language.code}
                  value={language.code}
                  onSelect={async (currentValue) => {
                    await setSetting(
                      "Language",
                      currentValue === value ? "" : currentValue
                    );
                    setOpen(false);
                  }}
                >
                  {language.name}
                  <Check
                    className={cn(
                      "ml-auto",
                      value === language.code ? "opacity-100" : "opacity-0"
                    )}
                  />
                </CommandItem>
              ))}
            </CommandGroup>
          </CommandList>
        </Command>
      </PopoverContent>
    </Popover>
  );
}
