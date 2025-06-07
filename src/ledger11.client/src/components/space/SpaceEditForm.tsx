import { useState } from "react";
import { Space } from "@/lib/types";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from "../ui/dialog";
import { Label } from "../ui/label";
import { Input } from "../ui/input";
import { Button } from "../ui/button";

interface SpaceEditFormProps {
  space: Space;
  onClose: (updatedSpace?: Space) => void;
}

export default function SpaceEditForm({ space, onClose }: SpaceEditFormProps) {
  const [name, setName] = useState(space.name ?? "");
  const [tint, setTint] = useState<string | undefined>(space.tint ?? "");
  const [currency, setCurrency] = useState(space.currency ?? "");

  const handleOpenChange = (open: boolean) => {
    if (!open) {
      onClose();
    }
  };

  const handleSubmit = () => {
    onClose({ ...space, name, tint, currency });
  };

  return (
    <Dialog open={true} onOpenChange={handleOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Edit Space</DialogTitle>
        </DialogHeader>
        <div className="space-y-4">
          <div>
            <Label htmlFor="name">Name</Label>
            <Input
              id="name"
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="Enter space name"
            />
          </div>
          <div>
            <Label htmlFor="tint">Tint</Label>
            <div className="flex items-center gap-2">
              <Input
                id="tint"
                type="color"
                value={tint ?? "#000000"} // fallback to a valid color
                onChange={(e) => setTint(e.target.value)}
              />
              <Button
                type="button"
                variant="link"
                onClick={() => setTint(undefined)}
              >
                Clear
              </Button>
            </div>
          </div>
          <div>
            <Label htmlFor="currency">Currency</Label>
            <Input
              id="currency"
              value={currency}
              onChange={(e) => setCurrency(e.target.value)}
              placeholder="e.g. USD"
            />
          </div>
        </div>
        <DialogFooter className="mt-4">
          <Button variant="outline" onClick={() => onClose()}>
            Cancel
          </Button>
          <Button onClick={handleSubmit}>Save</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
