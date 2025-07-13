import * as React from "react";
import { Check, ChevronsUpDown, X } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  Command,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
} from "@/components/ui/command";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover";
import { cn } from "@/lib/utils";

export interface MultiSelectOption {
  value: string;
  label: string;
  color?: string;
}

interface MultiSelectProps {
  options: MultiSelectOption[];
  selected: string[];
  onChange: (selected: string[]) => void;
  placeholder?: string;
  className?: string;
  allowSearch?: boolean;
  renderBadge?: (props: {
    option: MultiSelectOption;
    onRemove: () => void;
  }) => React.ReactNode;
}

export function MultiSelect({
  allowSearch,
  options,
  selected,
  onChange,
  placeholder = "Select options...",
  className,
  renderBadge,
}: MultiSelectProps) {
  const [open, setOpen] = React.useState(false);

  const handleSelect = (optionValue: string) => {
    if (selected.includes(optionValue)) {
      onChange(selected.filter((value) => value !== optionValue));
    } else {
      onChange([...selected, optionValue]);
    }
  };

  const handleRemove = (optionValue: string) => {
    onChange(selected.filter((value) => value !== optionValue));
  };

  const selectedOptions = options.filter((option) =>
    selected.includes(option.value)
  );

  return (
    <div className={className}>
      <Popover open={open} onOpenChange={setOpen}>
        <PopoverTrigger asChild>
          <Button
            variant="outline"
            role="combobox"
            aria-expanded={open}
            aria-label={placeholder}
            className="w-full justify-between !h-auto min-h-9"
          >
            <div className="flex flex-wrap gap-1 overflow-hidden">
              {selected.length === 0 && (
                <span className="text-muted-foreground">{placeholder}</span>
              )}
              {selectedOptions.length > 0 && (
                <div className="flex flex-wrap gap-1">
                  {selectedOptions.map((option) =>
                    renderBadge ? (
                      renderBadge({
                        option,
                        onRemove: () => handleRemove(option.value),
                      })
                    ) : (
                      <Badge
                        key={option.value}
                        variant="outline"
                        className="flex items-center gap-1"
                        style={
                          option.color ? { borderColor: option.color } : {}
                        }
                        onClick={(e) => {
                          e.stopPropagation();
                          handleRemove(option.value);
                        }}
                      >
                        {option.label}
                        <div
                          className="rounded-full outline-none focus:ring-2 focus:ring-ring"
                        >
                          <X className="h-3 w-3" />
                        </div>
                      </Badge>
                    )
                  )}
                </div>
              )}
            </div>
            <ChevronsUpDown className="ml-2 h-4 w-4 shrink-0 opacity-50" />
          </Button>
        </PopoverTrigger>
        <PopoverContent className="w-full p-0">
          <Command>
            {allowSearch && (<CommandInput placeholder="Search options..." />)}
            <CommandEmpty>No options found.</CommandEmpty>
            <CommandGroup className="max-h-60 overflow-auto">
              {options.map((option) => (
                <CommandItem
                  key={option.value}
                  value={option.value}
                  onSelect={() => handleSelect(option.value)}
                >
                  <Check
                    className={cn(
                      "mr-2 h-4 w-4",
                      selected.includes(option.value)
                        ? "opacity-100"
                        : "opacity-0"
                    )}
                  />
                  {option.label}
                </CommandItem>
              ))}
            </CommandGroup>
          </Command>
        </PopoverContent>
      </Popover>
    </div>
  );
}
