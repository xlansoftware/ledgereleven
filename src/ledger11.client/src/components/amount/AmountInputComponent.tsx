import { useRef, useState } from "react";
import { Input } from "../ui/input";
import { Button } from "../ui/button";
import { Transaction } from "@/lib/types";
import { ExchangeRateDialog } from "./ExchangeRateDialog";
import { parseMoneyInput } from "@/lib/parseMoneyInput";
import { useSpaceStore } from "@/lib/store-space";

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

  const { current } = useSpaceStore();

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
      if (amount.currency === (current?.currency || "USD")) {
        onConfirm({ value: amount.value });
        setValue("");
        return;
      }
      // allow the user to provide exchange rate
      setExchangeRateDialogProps({
        isOpen: true,
        ledgerCurrency: current?.currency,
        value: amount.value,
        currency: amount.currency,
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
        aria-label="Amount"
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
      {exchangeRateDialogProps.isOpen && (
        <ExchangeRateDialog
          onConfirm={(result) => {
            onConfirm({
              value: result.value,
              currency: result.currency,
              exchangeRate: result.exchangeRate,
            });
            setExchangeRateDialogProps({ isOpen: false });
            setValue("");
          }}
          onCancel={() => setExchangeRateDialogProps({ isOpen: false })}
          title="Exchange Rate"
          description={`Are you sure you want to add a transaction for ${value}?`}
          {...exchangeRateDialogProps}
        />
      )}
    </form>
  );
}
