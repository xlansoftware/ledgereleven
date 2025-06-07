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
import { Textarea } from "@/components/ui/textarea";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Button } from "@/components/ui/button";
import { Transaction, TransactionDetails } from "@/lib/types";
import { useCategoryStore } from "@/lib/store-category";
import { useTransactionStore } from "@/lib/store-transaction";
import { toast } from "sonner";

interface EditDetailFormProps {
  transaction: Transaction;
  detail: TransactionDetails;
  onClose: () => void;
}

export default function EditDetailForm({
  transaction,
  detail,
  onClose,
}: EditDetailFormProps) {
  const { categories } = useCategoryStore();
  const { updateTransaction } = useTransactionStore();

  const [editValues, setEditValues] = useState({
    value: `${detail.value}`,
    quantity: `${detail.quantity}`,
    description: detail.description || "",
    categoryId: detail.categoryId,
  });

  const saveEdit = async () => {
    const updatedTransaction = {
      ...transaction,
      transactionDetails: transaction.transactionDetails!.map((d) =>
        d.id === detail.id
          ? {
              ...d,
              value: editValues.value
                ? Number.parseFloat(editValues.value)
                : undefined,
              quantity: editValues.quantity
                ? Number.parseFloat(editValues.quantity)
                : undefined,
              description: editValues.description || undefined,
              category: editValues.categoryId || undefined,
            }
          : d
      ),
    };
    updatedTransaction.value = updatedTransaction.transactionDetails?.reduce(
      (acc, d) => acc + (d.value || 0) * (d.quantity || 1),
      0
    );
    await updateTransaction(transaction.id!, updatedTransaction);
    toast.success("Done!");
    onClose();
  };

  return (
    <Dialog open={true} onOpenChange={onClose}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Edit Detail</DialogTitle>
        </DialogHeader>

        <div className="space-y-4 py-4">
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label htmlFor="edit-amount">Value</Label>
              <Input
                id="edit-amount"
                type="number"
                value={editValues.value}
                onChange={(e) => {
                  const update = { ...editValues, value: e.target.value };
                  setEditValues(update);
                }}
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="edit-quantity">Quantity</Label>
              <Input
                id="edit-quantity"
                type="number"
                value={editValues.quantity}
                onChange={(e) => {
                  const update = { ...editValues, quantity: e.target.value };
                  setEditValues(update);
                }}
              />
            </div>
          </div>

          <div className="space-y-2">
            <Label htmlFor="edit-description">Description</Label>
            <Textarea
              id="edit-description"
              value={editValues.description}
              onChange={(e) =>
                setEditValues({ ...editValues, description: e.target.value })
              }
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="edit-category">Category</Label>
            <Select
              value={editValues.categoryId?.toString()}
              onValueChange={(value) =>
                setEditValues({ ...editValues, categoryId: Number(value) })
              }
            >
              <SelectTrigger>
                <SelectValue placeholder="Select a category" />
              </SelectTrigger>
              <SelectContent>
                {categories.map((cat) => (
                  <SelectItem key={cat.id} value={cat.id.toString()}>
                    {cat.name}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
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
