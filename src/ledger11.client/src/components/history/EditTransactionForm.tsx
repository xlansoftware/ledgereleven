"use client";

import { useState } from "react";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from "@/components/ui/dialog";
import { Label } from "@/components/ui/label";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { useBookStore } from "@/lib/store-book";
import { Transaction } from "@/lib/types";
import { toast } from "sonner";
import { ResponsiveSelect } from "../responsive/ResponsiveSelect";

interface EditTransactionFormProps {
  transaction: Transaction;
  onClose: () => void;
}

export default function EditTransactionForm({
  transaction,
  onClose,
}: EditTransactionFormProps) {
  const { categories, updateTransaction } = useBookStore();

  const [editValues, setEditValues] = useState({
    value: `${transaction.value}`,
    notes: transaction.notes ? `${transaction.notes}` : "",
    categoryId: transaction.categoryId,
    currency: transaction.currency,
    exchangeRate: transaction.exchangeRate,
  });

  const saveEdit = async () => {
    try {
      await updateTransaction(transaction.id!, {
        ...transaction,
        value: editValues.value
          ? Number.parseFloat(editValues.value)
          : undefined,
        notes: editValues.notes,
        categoryId: editValues.categoryId || undefined,
        currency: editValues.currency || undefined,
        exchangeRate: editValues.exchangeRate || undefined,
      });
      toast.success("Done!");
    } catch (error) {
      console.error("Error updating transaction:", error);
      toast.error("Error updating transaction");
    }
    onClose();
  };

  return (
    <Dialog defaultOpen={true} onOpenChange={onClose}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Edit Transaction</DialogTitle>
        </DialogHeader>

        <div className="space-y-4 py-4">
          <div className="space-y-2">
            <Label htmlFor="edit-total">Value</Label>
            <Input
              autoFocus
              id="edit-total"
              type="number"
              placeholder="0.00"
              value={editValues.value}
              onChange={(e) => {
                const update = { ...editValues, value: e.target.value };
                setEditValues(update);
              }}
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="edit-notes">Notes</Label>
            <Input
              id="edit-notes"
              type="text"
              placeholder="Notes"
              value={editValues.notes}
              onChange={(e) => {
                const update = { ...editValues, notes: e.target.value };
                setEditValues(update);
              }}
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="edit-category">Category</Label>
            <ResponsiveSelect
              value={editValues.categoryId?.toString()}
              onValueChange={(value) =>
                setEditValues({ ...editValues, categoryId: Number(value) })
              }
              options={categories.map((cat) => ({
                value: cat.id.toString(),
                label: cat.name,
              }))}
              placeholder="Select a category"
              title="Choose Category"
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="edit-currency">Currency</Label>
            <Input
              id="edit-currency"
              type="text"
              placeholder="USD"
              value={editValues.currency || ""}
              onChange={(e) => {
                const update = { ...editValues, currency: e.target.value };
                setEditValues(update);
              }}
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="edit-exchangeRate">Exchange Rate</Label>
            <Input
              id="edit-exchangeRate"
              type="number"
              placeholder="1.00"
              value={editValues.exchangeRate || ""}
              onChange={(e) => {
                const update = {
                  ...editValues,
                  exchangeRate: parseFloat(e.target.value),
                };
                setEditValues(update);
              }}
            />
          </div>
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={onClose}>
            Cancel
          </Button>
          <Button onClick={saveEdit}>Save Changes</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
