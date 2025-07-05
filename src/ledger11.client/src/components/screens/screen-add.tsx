import { useCategoryStore } from "@/lib/store-category";
import { useTransactionStore } from "@/lib/store-transaction";
import { useEffect, useRef, useState } from "react";
import { Input } from "../ui/input";
import { Button } from "../ui/button";
import { CategoryPicker } from "../category/CategoryPicker";
import { Category } from "@/lib/types";
import { ScrollArea } from "@/components/ui/scroll-area";
import { toast } from "sonner";
import { useSuccessOverlay } from "@/components/success";

const audio = new Audio("/sounds/success.mp3");

export default function AddScreen() {
  const { addTransaction } = useTransactionStore();
  const { categories, loadCategories } = useCategoryStore();
  const refInput = useRef<HTMLInputElement>(null);
  const [value, setValue] = useState("");
  const [notes, setNotes] = useState("");
  const [selectedCategory, setSelectedCategory] = useState<Category | null>(
    categories[0] || null
  );

  const { showSuccess } = useSuccessOverlay();

  useEffect(() => {
    if (categories.length === 0) {
      loadCategories().catch((error) => {
        console.error("Error loading categories:", error);
      });
    }
  }, [categories, loadCategories]);

  const handleAdd = async (e?: React.FormEvent) => {
    e?.preventDefault();

    if (!value) return;

    try {
      // disable controls
      await addTransaction({
        value: parseFloat(value),
        categoryId: selectedCategory?.id,
        notes,
        // transactionDetails: [{
        //   value: 10,
        //   description: "bread",
        //   quantity: 1,
        //   category: "Food"
        // }, {
        //   value: 20,
        //   description: "beer",
        //   quantity: 3,
        //   category: "Food"
        // }],
      });
      // reset controls
      setValue("");
      setNotes("");

      audio.play().catch((e) => console.warn("Playback blocked:", e));

      await showSuccess({ playSound: false });
    } catch (error) {
      console.error("Error adding transaction:", error);
      toast.error("Error adding transaction");
    }

    refInput.current?.focus();
  };

  return (
    <div className="flex flex-col gap-4 h-full justify-between">
      <form
        onSubmit={handleAdd}
        className="flex flex-col items-center gap-4 p-[2px] pt-4 w-full max-w-md mx-auto"
      >
        {/* Amount Input and Add Button */}
        <div className="flex w-full gap-3 items-center">
          <Input
            ref={refInput}
            autoFocus
            type="text"
            value={value}
            onChange={(e) => setValue(e.target.value)}
            placeholder="0.00"
            className="h-14 text-3xl font-semibold py-4 px-4 rounded-m border border-input shadow-sm focus-visible:ring-2 focus-visible:ring-primary transition-all w-full md:text-3xl lg:text-3xl"
          />
          <Button
            type="submit"
            className="h-14 px-6 text-lg font-medium rounded-m"
          >
            Add
          </Button>
        </div>

        {/* Notes Input */}
        <Input
          type="text"
          value={notes}
          onChange={(e) => setNotes(e.target.value)}
          placeholder="Notes ..."
          className="text-base py-3 px-4 rounded-m border border-input shadow-sm focus-visible:ring-2 focus-visible:ring-primary transition-all w-full"
        />
      </form>

      <ScrollArea className="flex-grow overflow-y-auto">
        <CategoryPicker
          categories={categories}
          selectedId={selectedCategory?.id}
          onSelect={(cat) => setSelectedCategory(cat)}
        />
      </ScrollArea>
    </div>
  );
}
