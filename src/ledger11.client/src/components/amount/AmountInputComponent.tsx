import { useRef, useState } from "react";
import { Input } from "../ui/input";
import { Button } from "../ui/button";
import { Transaction } from "@/lib/types";
import { ExchangeRateDialog } from "./ExchangeRateDialog";
import { parseMoneyInput } from "@/lib/parseMoneyInput";

interface AmountInputComponentProps {
  onConfirm: (transaction: Partial<Transaction>) => void;
}

interface ExchangeRateDialogProps {
  isOpen: boolean;
  ledgerCurrency?: string;
  value?: number;
  currency?: string;
}

export function AmountInputComponent({ onConfirm }: AmountInputComponentProps) {
  const refInput = useRef<HTMLInputElement>(null);

  const [value, setValue] = useState("");
  const [exchangeRateDialogProps, setExchangeRateDialogProps] =
    useState<ExchangeRateDialogProps>({ isOpen: false });

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setValue(e.target.value);
  };

  const handleAdd = (e?: React.FormEvent) => {
    e?.preventDefault();
    if (!value) return;

    const amount = parseMoneyInput(value);
    if (!amount) return;

    if (amount?.currency) {
      // allow the user to provide exchange rate
      debugger;
      setExchangeRateDialogProps({
        isOpen: true,
        ledgerCurrency: "EUR",
        value: amount.value,
        currency: amount.currency
      });
      // the call the onConfirm callback will be handled by the dialog
      return;
    }

    onConfirm({ value: amount.value });
    setValue("");
  };

  return (
    <form onSubmit={handleAdd} className="flex w-full gap-3 items-center">
      <Input
        ref={refInput}
        autoFocus
        type="text"
        value={value}
        onChange={handleInputChange}
        placeholder="0.00"
        className="h-14 text-3xl font-semibold py-4 px-4 rounded-m border border-input shadow-sm focus-visible:ring-2 focus-visible:ring-primary transition-all w-full md:text-3xl lg:text-3xl"
      />
      <Button type="submit" className="h-14 px-6 text-lg font-medium rounded-m">
        Add
      </Button>
      <ExchangeRateDialog
        onConfirm={() => {
          onConfirm({ value: parseFloat(value) });
          setExchangeRateDialogProps({ isOpen: false });
          setValue("");
        }}
        onCancel={() => setExchangeRateDialogProps({ isOpen: false })}
        title="Exchange Rate"
        description={`Are you sure you want to add a transaction for ${value}?`}
        {...exchangeRateDialogProps}
      />
    </form>
  );
}
