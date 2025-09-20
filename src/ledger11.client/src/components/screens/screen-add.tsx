import { useBookStore } from "@/lib/store-book";
import { useEffect, useState } from "react";
import { Input } from "../ui/input";
import { CategoryPicker } from "../category/CategoryPicker";
import { type Category, type Transaction } from "@/lib/types";
import { ScrollArea } from "@/components/ui/scroll-area";
import { toast } from "sonner";
import { useSuccessOverlay } from "@/components/success";
import { AmountInputComponent } from "../amount/AmountInputComponent";
import { playSound } from "@/lib/playSound";

export default function AddScreen() {
  const { addTransaction, categories } = useBookStore();
  const [notes, setNotes] = useState("");
  const [selectedCategory, setSelectedCategory] = useState<Category | null>(
    null
  );

  const { showSuccess } = useSuccessOverlay();

  useEffect(() => {
    // Set the first category as selected by default when they are loaded
    if (categories.length > 0 && !selectedCategory) {
      setSelectedCategory(categories[0]);
    }
  }, [categories, selectedCategory]);

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

      playSound(); // don't wait for finish
      await showSuccess({ playSound: false });
    } catch (error) {
      console.error("Error adding transaction:", error);
      toast.error("Error adding transaction");
    }
  };

  return (
    <div className="flex flex-col gap-4 h-full justify-between">
      <div
        className="flex flex-col items-center gap-4 p-[2px] w-full max-w-md mx-auto"
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
