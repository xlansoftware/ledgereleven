import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover";
import { Button } from "@/components/ui/button";
import { ScrollArea } from "@/components/ui/scroll-area";
import { iconMap, getIcon } from "@/lib/getIcon";
import { cn } from "@/lib/utils";

interface IconPickerProps {
  value?: string;
  onChange: (icon: string) => void;
}

export function IconPicker({ value, onChange }: IconPickerProps) {
  const Icon = getIcon(value);

  return (
    <Popover>
      <PopoverTrigger asChild>
        <Button variant="ghost" className="w-20 h-10 p-0">
          <Icon className="w-20 h-20" />
        </Button>
      </PopoverTrigger>
      <PopoverContent className="w-64 h-64 p-2">
        <ScrollArea className="h-full">
          <div className="grid grid-cols-5 gap-2">
            {Object.entries(iconMap).map(([key, IconComponent]) => (
              <button
                key={key}
                type="button"
                onClick={() => onChange(key)}
                className={cn(
                  "flex items-center justify-center w-10 h-10 rounded hover:bg-accent",
                  key === value && "bg-primary text-white"
                )}
              >
                <IconComponent className="w-5 h-5" />
              </button>
            ))}
          </div>
        </ScrollArea>
      </PopoverContent>
    </Popover>
  );
}
