import {
  Select,
  SelectTrigger,
  SelectValue,
  SelectContent,
  SelectItem,
} from "@/components/ui/select";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { useState } from "react";
import useIsMobile from "@/hooks/useIsMobile";

interface Option {
  label: string;
  value: string;
}

interface ResponsiveSelectProps {
  value?: string;
  onValueChange: (value: string) => void;
  options: Option[];
  placeholder?: string;
  title?: string;
}

export function ResponsiveSelect({
  value,
  onValueChange,
  options,
  placeholder = "Select an option",
  title = "Select an option",
}: ResponsiveSelectProps) {
  const isMobile = useIsMobile();
  const [dialogOpen, setDialogOpen] = useState(false);

  if (!isMobile) {
    return (
      <Select value={value} onValueChange={onValueChange}>
        <SelectTrigger>
          <SelectValue placeholder={placeholder} />
        </SelectTrigger>
        <SelectContent>
          {options.map((opt) => (
            <SelectItem key={opt.value} value={opt.value}>
              {opt.label}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>
    );
  }

  const currentLabel = options.find((opt) => opt.value === value)?.label;

  return (
    <>
      <div
        className="border rounded-md px-3 py-2 text-sm w-full cursor-pointer"
        onClick={() => setDialogOpen(true)}
      >
        {currentLabel || <span className="text-muted-foreground">{placeholder}</span>}
      </div>

      <Dialog open={dialogOpen} onOpenChange={setDialogOpen}>
        <DialogContent className="p-0">
          <DialogHeader>
            <DialogTitle className="p-4 pb-2 text-base font-medium">
              {title}
            </DialogTitle>
          </DialogHeader>
          <div className="divide-y max-h-[60vh] overflow-y-auto">
            {options.map((opt) => (
              <div
                key={opt.value}
                className="px-4 py-3 text-sm hover:bg-muted cursor-pointer"
                onClick={() => {
                  onValueChange(opt.value);
                  setDialogOpen(false);
                }}
              >
                {opt.label}
              </div>
            ))}
          </div>
        </DialogContent>
      </Dialog>
    </>
  );
}
