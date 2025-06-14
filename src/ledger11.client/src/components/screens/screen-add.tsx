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

  const handleAdd = (e?: React.FormEvent) => {
    e?.preventDefault();

    if (!value) return;

    // disable controls
    addTransaction({
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
    })
      .then(() => {
        // reset controls
        setValue("");
        setNotes("");
        showSuccess({ });
        // toast.success(`Added ${value} to ${selectedCategory?.name}`, {
        //   action: {
        //     label: "Undo",
        //     onClick: () => {
        //       toast.dismiss();
        //       removeTransaction(id)
        //         .then(() => {
        //           toast.success(
        //             `Removed ${value} from ${selectedCategory?.name}`
        //           );
        //         })
        //         .catch((error) => {
        //           console.error("Error removing transaction:", error);
        //         });
        //     },
        //   },
        // });
      })
      .catch((error) => {
        console.error("Error adding transaction:", error);
        toast.error("Error adding transaction");
      })
      .finally(() => {
        // re-enable controls
        refInput.current?.focus();
      });
  };

  return (
    <div className="flex flex-col gap-4 h-full justify-between">
      <form
        onSubmit={handleAdd}
        className="flex flex-col items-center gap-2 px-4 pt-1"
      >
        <div className="flex flex-row gap-2 items-center w-full">
          <Input
            ref={refInput}
            autoFocus
            className="text-xl p-6"
            type="number"
            // inputMode="decimal"
            // pattern="[0-9]*"
            value={value}
            onChange={(e) => setValue(e.target.value)}
            placeholder="0.00"
          />
          <Button type="submit">Add</Button>
        </div>
        <Input
          className="text-xl p-4"
          type="text"
          value={notes}
          onChange={(e) => setNotes(e.target.value)}
          placeholder="Notes ..."
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
