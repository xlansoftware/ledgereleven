"use client";

import { useState } from "react";
import {
  Dialog,
  DialogTrigger,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { toast } from "sonner";
import { type Space } from "@/lib/types";
import { formatCurrency } from "@/lib/utils";
import { Label } from "../ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "../ui/select";
import { getCurrencySetting } from "@/lib/spaceSettings";
import { Badge } from "../ui/badge";

interface SpaceMergeDialogProps {
  space: Space;
  spaces: Space[];
  trigger?: React.ReactNode;
  onMerge: (
    sourceSpaceId: string,
    targetSpaceId: string
  ) => Promise<void> | void;
  onCancel: () => void;
}

export default function SpaceMergeDialog({
  space,
  spaces,
  trigger,
  onMerge,
  onCancel,
}: SpaceMergeDialogProps) {
  const [targetSpaceId, setTargetSpaceId] = useState<string | undefined>(
    // Default to the other space if only two spaces exist
    spaces.length === 2 ? spaces.find((s) => s.id !== space.id)?.id : undefined
  );
  const [open, setOpen] = useState(true);
  const [loading, setLoading] = useState(false);

  const handleChangeSpace = async (value: string) => {
    setTargetSpaceId(value);
  };

  const handleMerge = async () => {
    if (!targetSpaceId) {
      toast.error("Please select a destination book.");
      return;
    }

    setLoading(true);
    try {
      await onMerge(space.id!, targetSpaceId);
      toast.success(`Book "${space.name}" merged successfully.`);
      setOpen(false);
    } catch (err) {
      console.error(err);
      toast.error("Failed to merge book.");
    } finally {
      setLoading(false);
    }
  };

  const handleOpenChange = (open: boolean) => {
    setOpen(open);
    if (!open) {
      setTargetSpaceId(undefined);
      onCancel();
    }
  };

  const selectedSpace = spaces.find((s) => s.id === targetSpaceId);

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      {trigger && <DialogTrigger asChild>{trigger}</DialogTrigger>}
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Merge {space.name}</DialogTitle>
          <DialogDescription>
            Create single record with value of{" "}
            <Badge variant={"secondary"}>
              {formatCurrency(space.totalValue ?? 0)}{" "}
              {getCurrencySetting(space)}
            </Badge>{" "}
            into another book and will mark this book as <Badge variant={"secondary"}>closed</Badge>.
          </DialogDescription>
        </DialogHeader>

        <div className="flex items-center gap-2">
          <Label htmlFor="space-select">Target Book:</Label>
          <Select
            value={selectedSpace?.id ?? ""}
            onValueChange={(value) => handleChangeSpace(value)}
          >
            <SelectTrigger className="w-[200px]" id="space-select">
              <SelectValue placeholder="Select a book" />
            </SelectTrigger>
            <SelectContent>
              {spaces.map((space) => (
                <SelectItem key={space.id} value={space.id!}>
                  {space.name}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        <DialogFooter>
          <Button onClick={() => handleOpenChange(false)} variant="secondary">
            Cancel
          </Button>
          <Button
            onClick={handleMerge}
            disabled={loading || !targetSpaceId}
            variant="destructive"
          >
            Merge
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
