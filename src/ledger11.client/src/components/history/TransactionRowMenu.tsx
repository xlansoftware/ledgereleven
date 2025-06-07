import { MoreHorizontal, Edit } from "lucide-react";
import { Button } from "../ui/button";
import ResponsiveMenu from "../responsive/ResponsiveMenu";
import { useState } from "react";
import { toast } from "sonner";
import { useTransactionStore } from "@/lib/store-transaction";
import EditTransactionForm from "./EditTransactionForm";
import { Transaction } from "@/lib/types";
import ConfirmMenuItem from "./ConfirmMenuItem";

export default function TransactionRowMenu({
  transaction,
}: {
  transaction: Transaction;
}) {
  const { removeTransaction } = useTransactionStore();
  const [edit, setEdit] = useState(false);

  const handleEdit = () => {
    requestAnimationFrame(() => setEdit(true));
  };

  const handleDelete = async () => {
    await removeTransaction(transaction.id!);
    toast.success("Deleted");
  };

  return (
    <>
      <ResponsiveMenu>
        <ResponsiveMenu.Trigger>
          <Button variant="ghost" size="icon">
            <MoreHorizontal className="h-4 w-4" />
            <span className="sr-only">Actions</span>
          </Button>
        </ResponsiveMenu.Trigger>
        <ResponsiveMenu.Content>
          <ResponsiveMenu.Item onClick={handleEdit}>
            <Edit className="h-4 w-4 mr-2" />
            Edit
          </ResponsiveMenu.Item>
          <ConfirmMenuItem
            title="Delete item"
            description="Are you sure you want to delete this item?"
            onConfirmed={handleDelete}
          />
        </ResponsiveMenu.Content>
      </ResponsiveMenu>

      {edit && (
        <EditTransactionForm
          transaction={transaction}
          onClose={() => setEdit(false)}
        />
      )}
    </>
  );
}
