import { useState } from "react";
import mexp from "math-expression-evaluator";
import { Transaction } from "@/lib/types";

interface UseAmountInputProps {
  onConfirm: (transaction: Partial<Transaction>) => void;
}

export function useAmountInput({ onConfirm }: UseAmountInputProps) {
  const [value, setValue] = useState("");
  const [isConfirmDialogOpen, setConfirmDialogOpen] = useState(false);

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setValue(e.target.value);
  };

  const evaluateExpression = () => {
    try {
      const result = (mexp as unknown as { eval: (value: string) => number }).eval(value);
      setValue(result.toString());
    } catch (error) {
      // ignore
    }
  };

  const confirm = (value: string) => {
    if (parseFloat(value) > 1000) {
      setConfirmDialogOpen(true);
    } else {
      onConfirm({ value: parseFloat(value) });
    }
  };

  return {
    value,
    handleInputChange,
    evaluateExpression,
    isConfirmDialogOpen,
    confirm,
    setConfirmDialogOpen,
  };
}
