import { PreferredLanguage } from "../PreferredLanguage";
import ScanReceiptButton from "../ScanReceiptButton";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "../ui/card";
import { Receipt } from "@/lib/types";
import { toast } from "sonner";
import { useState } from "react";
import { parsePurchaseRecords, receiptToTransaction } from "@/api";
import { useTransactionStore } from "@/lib/store-transaction";
import ConfirmTransactionForm from "../history/ConfirmTransactionForm";
import { useCategoryStore } from "@/lib/store-category";
import { Textarea } from "../ui/textarea";
import { Button } from "../ui/button";

export default function ScanScreen() {
  const { loadCategories } = useCategoryStore();
  const [showConfirmation, setShowConfirmation] = useState(0);
  const { addTransaction, removeTransaction } = useTransactionStore();

  const [description, setDescription] = useState("");

  const saveParsedRecords = async (receipt: Partial<Receipt>) => {
    const categories = await loadCategories();
    const transaction = receiptToTransaction(categories, receipt);
    if (!transaction) {
      toast.error("Could not understand... try another language?");
      return;
    }

    const id = await addTransaction(transaction);

    console.log("Master purchase ID:", id);
    setShowConfirmation(id);
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
    const receipt = await parsePurchaseRecords(description);
    saveParsedRecords(receipt);
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

        {showConfirmation !== 0 && (
          <ConfirmTransactionForm
            id={showConfirmation}
            onConfirm={() => {
              setShowConfirmation(0);
              toast.success("Done!");
            }}
            onUndo={() => {
              setShowConfirmation(0);
              removeTransaction(showConfirmation).catch((error) => {
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
