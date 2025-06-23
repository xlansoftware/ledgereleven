// import { Button } from "../ui/button";
// import { PlusCircleIcon } from "lucide-react";
// import { IncomeComponent } from "../widgets/IncomeComponent";

import { lazy, Suspense, useState } from "react";

const TotalByPeriodByCategoryComponent = lazy(
  () => import("@/components/insight/TotalByPeriodByCategoryComponent")
);

const HistoryComponent = lazy(
  () => import("@/components/insight/HistoryComponent")
);

export default function Insights() {

  const [tab, _] = useState("history");

  return (
    <Suspense>
      {tab === "history" && <HistoryComponent />}
      {tab === "total" && <TotalByPeriodByCategoryComponent />}
    </Suspense>
  );
}
