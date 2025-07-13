import { PreferredLanguage } from "../PreferredLanguage";
import ScanReceiptButton from "../ScanReceiptButton";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "../ui/card";
import { Receipt, Transaction } from "@/lib/types";
import { toast } from "sonner";
import { useState } from "react";
import { parsePurchaseRecords, receiptToTransaction } from "@/api";
import ConfirmTransactionForm from "../history/ConfirmTransactionForm";
import { Textarea } from "../ui/textarea";
import { Button } from "../ui/button";
import { useBookStore } from "@/lib/store-book";

export default function ScanScreen() {
  const [showConfirmation, setShowConfirmation] = useState<Transaction | null>(null);
  const { categories, addTransaction, removeTransaction } = useBookStore();

  const [description, setDescription] = useState("");

  const saveParsedRecords = async (receipt: Partial<Receipt>) => {
    const transaction = receiptToTransaction(categories, receipt);
    if (!transaction) {
      toast.error("Could not understand... try another language?");
      return;
    }

    const created = await addTransaction(transaction);

    console.log("Master purchase:", created);
    setShowConfirmation(created);
  };

  const handleScanCompletion = (receipt: Partial<Receipt>) => {
    console.log("Scan completed successfully in parent.");
    saveParsedRecords(receipt);
  };

  const handleParse = async () => {
    if (!description) return;
    // parse into Receipt object
    // const receipt: Receipt = {
    //   total_paid: "42",
    //   category: 'Pets',
    // };
    try
    {
      const receipt = await parsePurchaseRecords(description);
      saveParsedRecords(receipt);
    } catch (e: unknown) {
      // error instanceof Error ? error : new Error(String(error))
      toast.error(`Cound not do it: ${e}`);
    }
  };

  return (
    <div className="flex flex-col justify-center items-center px-4">
      <div className="w-full max-w-md space-y-6">

        <div className="pt-2">
          <ScanReceiptButton
            buttonText="Tap to Scan"
            onCompletion={handleScanCompletion}
            className="w-full py-6 text-lg rounded-xl"
          />
        </div>

        <h2 className="text-2xl font-bold text-center">Or Describe It With Words</h2>

        <div className="flex flex-col items-center gap-2">
          <Textarea
            rows={2}
            placeholder="What have you purchased..."
            value={description}
            onChange={(e) => setDescription(e.target.value)}
          />
          <Button onClick={handleParse}>Parse ...</Button>
        </div>


        <Card>
          <CardHeader>
            <CardTitle>Translate</CardTitle>
            <CardDescription>
              If you want to translate the receipt, select your preferred
              language.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <PreferredLanguage />
          </CardContent>
        </Card>

        {showConfirmation && (
          <ConfirmTransactionForm
            transaction={showConfirmation}
            onConfirm={() => {
              setShowConfirmation(null);
              toast.success("Done!");
            }}
            onUndo={() => {
              setShowConfirmation(null);
              removeTransaction(showConfirmation.id!).catch((error) => {
                console.error("Error removing transaction:", error);
                toast.error("Error removing transaction");
              });
            }}
          />
        )}
      </div>
    </div>
  );
}
