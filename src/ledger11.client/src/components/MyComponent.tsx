import { SwipeRow } from "./SwipeRow";
import { Trash2, Pen } from "lucide-react";

export const MyComponent = () => {
  const handleAction = (actionId: string) => {
    console.log("Action triggered:", actionId);
  };

  return (
    <SwipeRow
      leftActions={[]}
      rightActions={[
        { id: "edit", icon: <Pen />, backgroundColor: "#4CAF50" },
        { id: "delete", icon: <Trash2 />, backgroundColor: "#F44336" },
      ]}
      onAction={handleAction}
    >
      <div className="p-4 border-b">Swipe me!</div>
    </SwipeRow>
  );
};
