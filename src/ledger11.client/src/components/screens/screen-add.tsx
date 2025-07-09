import { useCategoryStore } from "@/lib/store-category";
import { useTransactionStore } from "@/lib/store-transaction";
import { useEffect, useState } from "react";
import { Input } from "../ui/input";
import { CategoryPicker } from "../category/CategoryPicker";
import { Category, Transaction } from "@/lib/types";
import { ScrollArea } from "@/components/ui/scroll-area";
import { toast } from "sonner";
import { useSuccessOverlay } from "@/components/success";
import { AmountInputComponent } from "../amount/AmountInputComponent";

const audio = new Audio("/sounds/success.mp3");

export default function AddScreen() {
  const { addTransaction } = useTransactionStore();
  const { categories, loadCategories } = useCategoryStore();
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

  const handleAdd = async (transaction: Partial<Transaction>) => {
    try {
      // console.log("Adding transaction:", transaction);
      // disable controls
      await addTransaction({
        ...transaction,
        categoryId: selectedCategory?.id,
        notes,
        // currency: "EUR",
        // exchangeRate: 1.1,
        // transactionDetails: [{
        //   value: 10,
        //   description: "bread",
        //   quantity: 1,
        // }, {
        //   value: 20,
        //   description: "beer",
        //   quantity: 3,
        // }],
      });
      // reset controls
      setNotes("");

      audio.play().catch((e) => console.warn("Playback blocked:", e));

      await showSuccess({ playSound: false });
    } catch (error) {
      console.error("Error adding transaction:", error);
      toast.error("Error adding transaction");
    }
  };

  return (
    <div className="flex flex-col gap-4 h-full justify-between">
      <div
        className="flex flex-col items-center gap-4 p-[2px] pt-4 w-full max-w-md mx-auto"
      >
        {/* Amount Input and Add Button */}
        <AmountInputComponent onConfirm={handleAdd} />

        {/* Notes Input */}
        <Input
          type="text"
          value={notes}
          onChange={(e) => setNotes(e.target.value)}
          aria-label="Notes"
          placeholder="Notes ..."
          className="text-base py-3 px-4 rounded-m border border-input shadow-sm focus-visible:ring-2 focus-visible:ring-primary transition-all w-full"
        />
      </div>

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
