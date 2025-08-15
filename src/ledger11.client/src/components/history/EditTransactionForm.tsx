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
import { parseMoneyInput } from "@/lib/parseMoneyInput";

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

  // console.log(JSON.stringify(editValues, null, 2));

  const saveEdit = (currentValues: typeof editValues) => {
    const computedValue = parseMoneyInput(currentValues.value);
    if (!computedValue) return currentValues;

    updateTransaction(transaction.id!, {
      ...transaction,
      value: computedValue.value,
      notes: currentValues.notes,
      categoryId: currentValues.categoryId || undefined,
      currency: computedValue.currency
        ? computedValue.currency
        : currentValues.currency || undefined,
      exchangeRate: currentValues.exchangeRate || undefined,
    })
      .then(() => {
        toast.success("Done!");
      })
      .catch((error) => {
        console.error("Error updating transaction:", error);
        toast.error("Error updating transaction");
      })
      .finally(() => {
        onClose();
      });
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
              type="text"
              placeholder="0.00"
              value={editValues.value}
              onChange={(e) => {
                setEditValues((prev) => ({
                  ...prev,
                  value: e.target.value,
                }));
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
                setEditValues((prev) => ({
                  ...prev,
                  notes: e.target.value,
                }));
              }}
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="edit-category">Category</Label>
            <ResponsiveSelect
              value={editValues.categoryId?.toString()}
              onValueChange={(value) =>
                setEditValues((prev) => ({
                  ...prev,
                  categoryId: Number(value),
                }))
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
                setEditValues((prev) => ({
                  ...prev,
                  currency: e.target.value,
                }));
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
                setEditValues((prev) => ({
                  ...prev,
                  exchangeRate: parseFloat(e.target.value),
                }));
              }}
            />
          </div>
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={onClose}>
            Cancel
          </Button>
          <Button onClick={() => { saveEdit(editValues); }}>Save Changes</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
