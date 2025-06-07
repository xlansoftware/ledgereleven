// components/responsive/ResponsiveIconPicker.tsx
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { ScrollArea } from "@/components/ui/scroll-area";
import { iconMap, getIcon } from "@/lib/getIcon";
import { cn } from "@/lib/utils";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover";
import { useState } from "react";
import useIsMobile from "@/hooks/useIsMobile";

interface IconPickerProps {
  value?: string;
  onChange: (icon: string) => void;
  title?: string;
}

export function ResponsiveIconPicker({
  value,
  onChange,
  title = "Choose an icon",
}: IconPickerProps) {
  const isMobile = useIsMobile();
  const [open, setOpen] = useState(false);
  const Icon = getIcon(value);

  const iconGrid = (
    <ScrollArea className="h-full">
      <div className="grid grid-cols-5 gap-2">
        {Object.entries(iconMap).map(([key, IconComponent]) => (
          <button
            key={key}
            type="button"
            onClick={() => {
              onChange(key);
              setOpen(false);
            }}
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
  );

  if (!isMobile) {
    return (
      <Popover open={open} onOpenChange={setOpen}>
        <PopoverTrigger asChild>
          <Button variant="ghost" className="w-14 h-10 p-0">
            <Icon className="w-6 h-6" />
          </Button>
        </PopoverTrigger>
        <PopoverContent className="w-64 h-64 p-2">{iconGrid}</PopoverContent>
      </Popover>
    );
  }

  return (
    <>
      <Button
        variant="ghost"
        className="w-14 h-10 p-0"
        onClick={() => setOpen(true)}
      >
        <Icon className="w-6 h-6" />
      </Button>

      <Dialog open={open} onOpenChange={setOpen}>
        <DialogContent className="p-0">
          <DialogHeader>
            <DialogTitle className="p-4 pb-2 text-base font-medium">
              {title}
            </DialogTitle>
          </DialogHeader>
          <div className="h-full p-2 pt-0">{iconGrid}</div>
        </DialogContent>
      </Dialog>
    </>
  );
}
