import { useBookStore } from "@/lib/store-book";
import { Category } from "@/lib/types";
import { useState } from "react";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogTrigger,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
  DialogDescription,
} from "@/components/ui/dialog";
import {
  Select,
  SelectTrigger,
  SelectValue,
  SelectContent,
  SelectItem,
} from "@/components/ui/select";
import { toast } from "sonner";

interface Props {
  category: Category;
  trigger: React.ReactNode;
  onConfirm: (replacementId: number) => void;
}

export default function CategoryReplaceDialog({
  category,
  trigger,
  onConfirm,
}: Props) {
  const { categories } = useBookStore();
  const [open, setOpen] = useState(false);
  const [replacementId, setReplacementId] = useState<number | null>(null);

  const otherCategories = categories.filter((c) => c.id !== category.id);

  const handleConfirm = () => {
    if (replacementId === null) {
      toast.error("Please select a replacement category");
      return;
    }

    onConfirm(replacementId);
    setOpen(false);
  };

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger asChild>{trigger}</DialogTrigger>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Replace & Delete Category</DialogTitle>
          <DialogDescription>
            You’re about to delete <strong>{category.name}</strong>. Select a category
            to reassign existing transactions.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-2">
          <Select onValueChange={(val) => setReplacementId(Number(val))}>
            <SelectTrigger>
              <SelectValue placeholder="Select replacement category" />
            </SelectTrigger>
            <SelectContent className="max-h-[40vh] overflow-y-auto">
              {otherCategories.map((c) => (
                <SelectItem key={c.id} className="py-2 px-4"  value={String(c.id)}>
                  {c.name}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={() => setOpen(false)}>
            Cancel
          </Button>
          <Button onClick={handleConfirm} disabled={replacementId === null}>
            Confirm
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
