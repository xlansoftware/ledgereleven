import { Transaction, TransactionDetails } from "@/lib/types";
import { Button } from "../ui/button";
import { Edit, MoreHorizontal } from "lucide-react";
import { useState } from "react";
import { useBookStore } from "@/lib/store-book";
import { toast } from "sonner";
import EditDetailForm from "./EditDetailForm";

import ResponsiveMenu from "../responsive/ResponsiveMenu";
import ConfirmMenuItem from "./ConfirmMenuItem";

interface DetailRowMenuProps {
  transaction: Transaction;
  detail: TransactionDetails;
}

export default function DetailRowMenu({
  transaction,
  detail,
}: DetailRowMenuProps) {
  const { updateTransaction } = useBookStore();
  const [edit, setEdit] = useState(false);

  const handleEdit = () => {
    requestAnimationFrame(() => {
      setEdit(true);
    });
  };

  const handleDelete = async () => {
    transaction.transactionDetails = transaction.transactionDetails?.filter(
      (d) => d.id !== detail.id
    );
    transaction.value =
      transaction.transactionDetails?.reduce(
        (acc, d) => acc + (d.value || 0) * (d.quantity || 1),
        0
      ) || 0;

    await updateTransaction(transaction.id!, transaction);
    toast.success("Done!");
  };

  return (
    <>
      <ResponsiveMenu>
        <ResponsiveMenu.Trigger>
          <Button
            variant="ghost"
            size="icon"
            onClick={(e) => e.stopPropagation()}
          >
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
        <EditDetailForm
          transaction={transaction}
          detail={detail}
          onClose={() => setEdit(false)}
        />
      )}
    </>
  );
}
