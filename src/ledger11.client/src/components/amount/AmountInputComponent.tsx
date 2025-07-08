import { useRef } from "react";
import { Input } from "../ui/input";
import { Button } from "../ui/button";
import { Transaction } from "@/lib/types";
import { useAmountInput } from "./useAmountInput";
import { ConfirmDialog } from "./ConfirmDialog";

interface AmountInputComponentProps {
  onConfirm: (transaction: Partial<Transaction>) => void;
}

export function AmountInputComponent({ onConfirm }: AmountInputComponentProps) {
  const refInput = useRef<HTMLInputElement>(null);
  const {
    value,
    handleInputChange,
    evaluateExpression,
    isConfirmDialogOpen,
    confirm,
    setConfirmDialogOpen,
  } = useAmountInput({ onConfirm });

  const handleAdd = (e?: React.FormEvent) => {
    e?.preventDefault();
    if (!value) return;

    confirm(value);
  };

  return (
    <form onSubmit={handleAdd} className="flex w-full gap-3 items-center">
      <Input
        ref={refInput}
        autoFocus
        type="text"
        value={value}
        onChange={handleInputChange}
        onBlur={evaluateExpression}
        placeholder="0.00"
        className="h-14 text-3xl font-semibold py-4 px-4 rounded-m border border-input shadow-sm focus-visible:ring-2 focus-visible:ring-primary transition-all w-full md:text-3xl lg:text-3xl"
      />
      <Button type="submit" className="h-14 px-6 text-lg font-medium rounded-m">
        Add
      </Button>
      <ConfirmDialog
        isOpen={isConfirmDialogOpen}
        onConfirm={() => {
          onConfirm({ value: parseFloat(value) });
          setConfirmDialogOpen(false);
        }}
        onCancel={() => setConfirmDialogOpen(false)}
        title="Confirm Transaction"
        description={`Are you sure you want to add a transaction for ${value}?`}
      />
    </form>
  );
}
