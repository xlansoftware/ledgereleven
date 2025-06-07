"use client";

import { useCategoryStore } from "@/lib/store-category";
import { useEffect, useState } from "react";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { toast } from "sonner";
import { Trash2Icon, ChevronUp, ChevronDown } from "lucide-react";
import CategoryEditor from "../category/CategoryEditor";
import CategoryReplaceDialog from "../category/CategoryReplaceForm";
import { fetchWithAuth } from "@/api";
import { Badge } from "../ui/badge";
import { motion, AnimatePresence } from "framer-motion";

export default function CategoriesScreen() {
  const { categories, addCategory, removeCategory, loadCategories } =
    useCategoryStore();

  const [order, setOrder] = useState<number[]>([]);
  const [newCategoryName, setNewCategoryName] = useState("");
  const [reordering, setReordering] = useState(false);

  useEffect(() => {
    loadCategories().then((c) => setOrder(c.map((_, index) => index)));
  }, [loadCategories]);

  const handleAdd = async () => {
    if (!newCategoryName.trim()) return;
    try {
      await addCategory({ name: newCategoryName, displsyOrder: 0 });
      toast.success("Category added");
      setNewCategoryName("");
    } catch (err) {
      toast.error("Failed to add category");
      console.error(err);
    }
  };

  const handleRemove = async (id: number, replaceWithId?: number) => {
    try {
      const c = await removeCategory(id, replaceWithId);
      setOrder(c.map((_, index) => index))
      toast.success("Category removed");
    } catch (err) {
      toast.error("Remove failed");
      console.error(err);
    }
  };

  const handleReorder = async () => {
    if (reordering) {
      try {
        await fetchWithAuth("/api/category/reorder", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify(order.map((index) => categories[index].id)),
        });
        toast.success("Reorder saved");
      } catch (err) {
        toast.error("Reorder failed");
        console.error(err);
      }
      setReordering(false);
    } else {
      setReordering(true);
    }
  };

  const moveUp = (index: number) => {
    if (index <= 0) return;
    setOrder((prev) => {
      const newOrder = [...prev];
      [newOrder[index - 1], newOrder[index]] = [
        newOrder[index],
        newOrder[index - 1],
      ];
      return newOrder;
    });
  };

  const moveDown = (index: number) => {
    if (index >= order.length - 1) return;
    setOrder((prev) => {
      const newOrder = [...prev];
      [newOrder[index + 1], newOrder[index]] = [
        newOrder[index],
        newOrder[index + 1],
      ];
      return newOrder;
    });
  };

  return (
    <div className="p-2 space-y-4">
      <div className="flex items-center gap-2">
        {reordering && <div className="flex-grow" />}
        {!reordering && (
          <>
            <Input
              placeholder="New category name"
              value={newCategoryName}
              onChange={(e) => setNewCategoryName(e.target.value)}
            />
            <Button onClick={handleAdd}>Add</Button>
          </>
        )}

        <Button variant="secondary" onClick={handleReorder}>
          {reordering ? "Done" : "Reorder"}
        </Button>
      </div>

      <div className="space-y-4">
        <AnimatePresence>
          {order.map((index, i) => {
            const category = categories[index];
            if (!category) return null; // Handle case where category might not exist
            return (
              <motion.div
                key={category.id}
                layout
                transition={{ type: "spring", stiffness: 500, damping: 30 }}
                // className="flex items-center gap-2 p-2 border rounded bg-white shadow-sm transition-all"
              >
                {reordering && (
                  <div className="flex flex-row gap-4 items-center">
                    <Button
                      size="icon"
                      onClick={() => moveUp(i)}
                      disabled={i === 0}
                    >
                      <ChevronUp />
                    </Button>
                    <Button
                      size="icon"
                      onClick={() => moveDown(i)}
                      disabled={i === order.length - 1}
                    >
                      <ChevronDown />
                    </Button>
                    <Badge variant={"secondary"}>{i + 1}</Badge>
                    <div>{category.name}</div>
                  </div>
                )}
                {!reordering && (
                  <div className="flex flex-row gap-4 items-center">
                    <CategoryEditor category={category} />
                    <CategoryReplaceDialog
                      category={category}
                      trigger={
                        <Button variant="outline">
                          <Trash2Icon />
                        </Button>
                      }
                      onConfirm={(replacementId) => {
                        if (!replacementId) return;
                        handleRemove(category.id, replacementId);
                      }}
                    />
                  </div>
                )}
              </motion.div>
            );
          })}
        </AnimatePresence>
      </div>
    </div>
  );
}
