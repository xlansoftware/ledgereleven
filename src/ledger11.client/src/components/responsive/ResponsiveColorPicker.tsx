// components/responsive/ResponsiveColorPicker.tsx
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover";
import { cn } from "@/lib/utils";
import { useState } from "react";
import useIsMobile from "@/hooks/useIsMobile";

interface ResponsiveColorPickerProps {
  value?: string;
  onChange: (color: string) => void;
  title?: string;
}

const COLORS = [
  "#FF6B6B", "#FCA311", "#FFD93D", "#6BCB77", "#4D96FF", "#9D4EDD", "#FF5C8A", "#00B8A9",
  "#FF8C42", "#F3722C", "#90BE6D", "#43AA8B", "#577590", "#118AB2", "#8338EC", "#FF006E",
  "#E29578", "#FFB627", "#A1C181", "#1D3557", "#2EC4B6", "#FF9F1C", "#6A0572", "#F72585",
  "#06D6A0", "#FFD166", "#EF476F", "#D90368", "#7209B7", "#3A86FF", "#FF70A6", "#70D6FF",
  "#A9DEF9", "#FDFFB6", "#CAFFBF", "#BDB2FF", "#FFC6FF", "#FFADAD", "#FFB5A7", "#B5E48C",
  "#99D98C", "#52B788", "#34A0A4", "#168AAD", "#52796F", "#FF9770", "#FBC4AB", "#CDB4DB",
  "#D0F4DE", "#FFDDD2", "#FFCAD4", "#FF7B9C", "#D4A5A5", "#A3C9A8", "#F2C6B4", "#7BDFF2",
  "#B2F7EF", "#EFF7F6", "#FFE156", "#6A4C93", "#1982C4", "#8AC926", "#FF595E", "#FFCA3A"
];

export function ResponsiveColorPicker({
  value,
  onChange,
  title = "Pick a color",
}: ResponsiveColorPickerProps) {
  const isMobile = useIsMobile();
  const [open, setOpen] = useState(false);

  const colorGrid = (
    <div className="grid grid-cols-8 gap-4">
      {COLORS.map((color) => (
        <button
          key={color}
          type="button"
          onClick={() => {
            onChange(color);
            setOpen(false);
          }}
          className={cn(
            "w-6 h-6 rounded-sm border border-muted shadow",
            color === value ? "ring-2 ring-ring ring-offset-1" : ""
          )}
          style={{ backgroundColor: color }}
        />
      ))}
    </div>
  );

  if (!isMobile) {
    return (
      <Popover open={open} onOpenChange={setOpen}>
        <PopoverTrigger asChild>
          <Button
            variant="ghost"
            className="w-10 h-10 p-0 border"
            style={{ backgroundColor: value }}
          />
        </PopoverTrigger>
        <PopoverContent className="p-2">{colorGrid}</PopoverContent>
      </Popover>
    );
  }

  return (
    <>
      <Button
        variant="ghost"
        className="w-10 h-10 p-0 border"
        onClick={() => setOpen(true)}
        style={{ backgroundColor: value }}
      />

      <Dialog open={open} onOpenChange={setOpen}>
        <DialogContent className="p-4">
          <DialogHeader>
            <DialogTitle className="text-base font-medium mb-2">
              {title}
            </DialogTitle>
          </DialogHeader>
          {colorGrid}
        </DialogContent>
      </Dialog>
    </>
  );
}
