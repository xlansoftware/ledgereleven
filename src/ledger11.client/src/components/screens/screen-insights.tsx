// import { Button } from "../ui/button";
// import { PlusCircleIcon } from "lucide-react";
// import { IncomeComponent } from "../widgets/IncomeComponent";

import { lazy, Suspense, useState } from "react";
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { BarChart3Icon, ChartPieIcon } from "lucide-react";

const TotalByPeriodByCategoryComponent = lazy(
  () => import("@/components/insight/TotalByPeriodByCategoryComponent")
);

const HistoryComponent = lazy(
  () => import("@/components/insight/HistoryComponent")
);

export default function Insights() {
  const [tab, setTab] = useState("total");
  return (
    <>
      <div className="container flex items-center justify-center w-full">
        <Tabs value={tab} className="pr-4" onValueChange={(value) => setTab(value)}>
          <TabsList>
            <TabsTrigger value="total" className="m-4">
              <ChartPieIcon className="w-6 h-6" />
            </TabsTrigger>
            <TabsTrigger value="history" className="m-4">
              <BarChart3Icon className="w-6 h-6" />
            </TabsTrigger>
          </TabsList>
        </Tabs>
      </div>
      <Suspense>
        {tab === "history" && <HistoryComponent />}
        {tab === "total" && <TotalByPeriodByCategoryComponent />}
      </Suspense>
    </>
  );
}
